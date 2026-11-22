using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Focus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Focus.Api.IntegrationTests;

public sealed class SessionWorkflowTests
{
    internal const string StubPassword = "fixture-password-74!";
    internal const string Sessions = "/api/v1/sessions";

    [PostgresFact]
    public async Task RegistersAndRotatesAndRevokesCredentials()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync();
        using var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Sessions)).StatusCode);
        await RegisterAsync(client, "owner@example.invalid");
        await ExpectAsync(HttpStatusCode.Conflict, client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "OWNER@example.invalid", password = StubPassword, displayName = "Duplicate" }));
        await ExpectAsync(HttpStatusCode.BadRequest, client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "short@example.invalid", password = "short", displayName = "Short" }));
        await ExpectAsync(HttpStatusCode.Unauthorized, client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "owner@example.invalid", password = "incorrect" }));
        var tokens = await LoginAsync(client, "owner@example.invalid");
        Authorize(client, tokens);

        var options = api.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(BearerTokenDefaults.AuthenticationScheme);
        var ticket = options.BearerTokenProtector.Unprotect(tokens.AccessToken)!;
        ticket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        Authorize(client, tokens with { AccessToken = options.BearerTokenProtector.Protect(ticket) });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Authorize(client, tokens);

        var requests = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync(
            "/api/v1/auth/refresh", new { tokens.RefreshToken })));
        var renewed = (await Assert.Single(requests, r => r.IsSuccessStatusCode).Content.ReadFromJsonAsync<Tokens>())!;
        Assert.Single(requests, r => r.StatusCode == HttpStatusCode.Unauthorized);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Authorize(client, renewed);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        await ExpectAsync(HttpStatusCode.NoContent, client.PostAsync("/api/v1/auth/logout", null));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var accountLogin = await LoginAsync(client, "owner@example.invalid");
        Authorize(client, accountLogin);
        const string newPassword = "changed-fixture-passphrase!";
        await ExpectAsync(HttpStatusCode.NoContent, client.PostAsJsonAsync("/api/v1/auth/me/change-password",
            new { currentPassword = StubPassword, newPassword }));
        await ExpectAsync(HttpStatusCode.Unauthorized, client.GetAsync("/api/v1/auth/me"));
        await ExpectAsync(HttpStatusCode.Unauthorized, client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { accountLogin.RefreshToken }));
        client.DefaultRequestHeaders.Authorization = null;
        await ExpectAsync(HttpStatusCode.Unauthorized, client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "owner@example.invalid", password = StubPassword }));
        await ExpectAsync(HttpStatusCode.OK, client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "owner@example.invalid", password = newPassword }));
    }

    [PostgresFact]
    public async Task SendsCorsAndTraceHeadersWithSafeValidationAndRateLimitErrors()
    {
        await using var api = new PostgresApi();
        using var client = api.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, Sessions);
        preflight.Headers.Add("Origin", "https://focus.test");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        using var cors = await client.SendAsync(preflight);
        Assert.Equal(HttpStatusCode.NoContent, cors.StatusCode);
        Assert.Equal("https://focus.test", Assert.Single(cors.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(cors.Headers.Contains("Access-Control-Allow-Credentials"));

        using var bad = await ExpectAsync(HttpStatusCode.BadRequest,
            client.PostAsJsonAsync("/api/v1/auth/login", new { email = "reject-private-value", password = "never-echo-this" }));
        Assert.Equal("application/problem+json", bad.Content.Headers.ContentType?.MediaType);
        var trace = Assert.Single(bad.Headers.GetValues("X-Request-Id"));
        var raw = await bad.Content.ReadAsStringAsync();
        Assert.DoesNotContain("never-echo-this", raw);
        Assert.DoesNotContain("reject-private-value", raw);
        Assert.Contains(trace, raw);

        for (var i = 0; i < 19; i++)
        {
            using var invalid = await client.PostAsJsonAsync("/api/v1/auth/login", new { });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }

        using var limited = await ExpectAsync(HttpStatusCode.TooManyRequests,
            client.PostAsJsonAsync("/api/v1/auth/login", new { }));
        Assert.NotNull(limited.Headers.RetryAfter);
        Assert.Equal(429, (await JsonAsync(limited)).GetProperty("status").GetInt32());
    }

    [PostgresFact]
    public async Task WorkflowPersistsLifecycleAndLimitsEachUserToTheirOwnResources()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync();
        using var owner = api.CreateClient();
        using var stranger = api.CreateClient();
        await RegisterAsync(owner, "owner@example.invalid");
        await RegisterAsync(stranger, "stranger@example.invalid");
        Authorize(owner, await LoginAsync(owner, "owner@example.invalid"));
        Authorize(stranger, await LoginAsync(stranger, "stranger@example.invalid"));

        var created = await ExpectAsync(HttpStatusCode.Created,
            owner.PostAsJsonAsync(Sessions, new { plannedDurationSeconds = 1500, goals = new[] { "Draft" } }));
        var session = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var resource = $"{Sessions}/{session}";
        Assert.EndsWith(resource, created.Headers.Location!.ToString());
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(resource));
        await ExpectAsync(HttpStatusCode.NotFound, stranger.PostAsync(resource + "/start", null));
        await ExpectAsync(HttpStatusCode.BadRequest, owner.GetAsync(Sessions + "?sort=unknown"));
        await ExpectAsync(HttpStatusCode.BadRequest, owner.PostAsJsonAsync(Sessions, new { plannedDurationSeconds = 0 }));
        var goal = await ExpectAsync(HttpStatusCode.Created,
            owner.PostAsJsonAsync(resource + "/goals", new { description = "Review" }));
        var goalId = (await goal.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await ExpectAsync(HttpStatusCode.OK, owner.PostAsync(resource + "/start", null));
        await ExpectAsync(HttpStatusCode.OK, owner.PostAsync(resource + "/pause", null));
        await ExpectAsync(HttpStatusCode.OK, owner.PostAsync(resource + "/resume", null));
        var completion = new
        {
            selfReportedConcentration = 4,
            selfReportedProductivity = 2,
            goalOutcomes = new[] { new { goalId, status = "PartiallyCompleted" } }
        };

        var simultaneous = await Task.WhenAll(Enumerable.Range(0, 2).Select(
            _ => owner.PostAsJsonAsync(resource + "/complete", completion)));
        Assert.Single(simultaneous, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(simultaneous, x => x.StatusCode == HttpStatusCode.Conflict);

        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FocusDbContext>();
        Assert.Equal(4, (await db.FocusSessions.AsNoTracking().SingleAsync()).SelfReportedConcentration);
        Assert.Equal(2, (await db.FocusSessions.AsNoTracking().SingleAsync()).SelfReportedProductivity);
        var history = await owner.GetFromJsonAsync<JsonElement>(Sessions + "?status=Completed&pageSize=1");
        Assert.Equal(1, history.GetProperty("totalCount").GetInt64());
        Assert.Equal(0, (await stranger.GetFromJsonAsync<JsonElement>(Sessions)).GetProperty("totalCount").GetInt64());
    }

    [PostgresFact]
    public async Task UpgradesFromPreviouslyTrackedMigrationsWithoutDroppingExistingRows()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync("20261115233249_AddTelemetryAndResearchData");
        using var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);

        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FocusDbContext>();
        var legacy = new Focus.Domain.Entities.User("legacy@example.invalid", "Legacy");
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Users" ("Id", "Email", "DisplayName", "CreatedAt")
            VALUES ({legacy.Id}, {legacy.Email}, {legacy.DisplayName}, {legacy.CreatedAt})
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "FocusSessions" ("Id", "UserId", "PlannedDuration", "Status", "CreatedAt")
            VALUES ({Guid.NewGuid()}, {legacy.Id}, {TimeSpan.FromMinutes(10)}, {0}, {DateTimeOffset.UtcNow})
            """);

        await api.MigrateAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(legacy.Id, (await db.FocusSessions.AsNoTracking().SingleAsync()).UserId);
    }

    internal static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email) =>
        ExpectAsync(HttpStatusCode.Created, client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = StubPassword, displayName = "Test Account" }));

    internal static async Task<Tokens> LoginAsync(HttpClient client, string email)
    {
        var response = await ExpectAsync(HttpStatusCode.OK, client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email, password = StubPassword }));
        return (await response.Content.ReadFromJsonAsync<Tokens>())!;
    }

    internal static void Authorize(HttpClient client, Tokens tokens) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

    internal static async Task<HttpResponseMessage> ExpectAsync(HttpStatusCode expected, Task<HttpResponseMessage> request)
    {
        var response = await request;
        Assert.True(expected == response.StatusCode,
            $"Expected {expected}, received {response.StatusCode} for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.AbsolutePath}.");
        return response;
    }

    internal static Task<JsonElement> JsonAsync(HttpResponseMessage response) => response.Content.ReadFromJsonAsync<JsonElement>();

    internal sealed record Tokens(string AccessToken, string RefreshToken);
}
