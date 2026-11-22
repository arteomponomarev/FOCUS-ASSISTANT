using System.Net;
using System.Net.Http.Json;
using System.Text;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using static Focus.Api.IntegrationTests.SessionWorkflowTests;

namespace Focus.Api.IntegrationTests;

public sealed class ReleaseVerificationTests
{
    [PostgresFact]
    public async Task HostCompletesGracefulShutdown()
    {
        await using var api = new PostgresApi();
        using var client = api.CreateClient();
        await ExpectAsync(HttpStatusCode.OK, client.GetAsync("/health/live"));
        var lifetime = api.Services.GetRequiredService<IHostApplicationLifetime>();
        await api.DisposeAsync();
        Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
        Assert.True(lifetime.ApplicationStopped.IsCancellationRequested);
    }

    [PostgresFact]
    public async Task RejectsAmbiguousAndMalformedRequests()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync();
        using var client = api.CreateClient();
        await RegisterAsync(client, "validation@example.invalid");
        Authorize(client, await LoginAsync(client, "validation@example.invalid"));

        await ExpectAsync(HttpStatusCode.UnsupportedMediaType,
            client.PostAsync(Sessions, new StringContent("{}", Encoding.UTF8, "text/plain")));
        await ExpectAsync(HttpStatusCode.BadRequest,
            client.PostAsync(Sessions, JsonContent("""{"plannedDurationSeconds":""")));
        await ExpectAsync(HttpStatusCode.BadRequest,
            client.PostAsync(Sessions, JsonContent("""{"plannedDurationSeconds":0,"plannedDurationSeconds":600}""")));
        await ExpectAsync(HttpStatusCode.BadRequest, client.GetAsync(Sessions + "/invalid-guid"));
        await ExpectAsync(HttpStatusCode.BadRequest, client.GetAsync(Sessions + "?status=unknown"));

        var created = await ExpectAsync(HttpStatusCode.Created,
            client.PostAsJsonAsync(Sessions, new { plannedDurationSeconds = 100 }));
        var target = Sessions + "/" + (await JsonAsync(created)).GetProperty("id").GetGuid();
        await ExpectAsync(HttpStatusCode.Conflict, client.PostAsync(target + "/resume", null));
        await ExpectAsync(HttpStatusCode.OK, client.PostAsync(target + "/start", null));
        await ExpectAsync(HttpStatusCode.BadRequest, client.PostAsJsonAsync(target + "/complete",
            new { selfReportedConcentration = 6, selfReportedProductivity = 4 }));
        var current = await JsonAsync(await ExpectAsync(HttpStatusCode.OK, client.GetAsync(target)));
        Assert.Equal("Active", current.GetProperty("status").GetString());
    }

    [PostgresFact]
    public async Task PostgreSqlEnforcesOwnershipIdempotencyAndOpenPauseUniqueness()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync();
        using var client = api.CreateClient();
        await RegisterAsync(client, "constraints@example.invalid");
        Authorize(client, await LoginAsync(client, "constraints@example.invalid"));
        var created = await ExpectAsync(HttpStatusCode.Created,
            client.PostAsJsonAsync(Sessions, new { plannedDurationSeconds = 100 }));
        var session = (await JsonAsync(created)).GetProperty("id").GetGuid();
        var path = Sessions + "/" + session;
        await ExpectAsync(HttpStatusCode.OK,
            client.PatchAsJsonAsync("/api/v1/settings/privacy", new { telemetryEnabled = true }));
        var started = await JsonAsync(await ExpectAsync(HttpStatusCode.OK, client.PostAsync(path + "/start", null)));
        var now = started.GetProperty("startedAt").GetDateTimeOffset().AddSeconds(1);
        api.Clock.Advance(TimeSpan.FromSeconds(10));
        await ExpectAsync(HttpStatusCode.OK, client.PostAsJsonAsync(path + "/activity/batch", new
        {
            items = new[]
            {
                new
                {
                    clientEventId = Guid.NewGuid(),
                    startedAt = now,
                    endedAt = now.AddSeconds(2),
                    keyboardEventCount = 0,
                    mouseClickCount = 0,
                    windowSwitchCount = 0,
                    idleSeconds = 0
                }
            }
        }));
        await ExpectAsync(HttpStatusCode.OK, client.PostAsync(path + "/pause", null));

        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FocusDbContext>();

        var foreignKey = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SessionGoals" ("Id","FocusSessionId","Description","Status")
            VALUES ({Guid.NewGuid()}, {Guid.NewGuid()}, {"Unknown session"}, {0})
            """));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, foreignKey.SqlState);

        var required = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SessionGoals" ("Id","FocusSessionId","Status")
            VALUES ({Guid.NewGuid()}, {session}, {0})
            """));
        Assert.Equal(PostgresErrorCodes.NotNullViolation, required.SqlState);

        var duplicateEvent = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ActivityAggregates"
                ("Id","FocusSessionId","ClientEventId","StartedAt","EndedAt","KeyboardEventCount","MouseClickCount","WindowSwitchCount","IdleSeconds")
            SELECT {Guid.NewGuid()},"FocusSessionId","ClientEventId","StartedAt","EndedAt","KeyboardEventCount","MouseClickCount","WindowSwitchCount","IdleSeconds"
            FROM "ActivityAggregates" WHERE "FocusSessionId" = {session}
            """));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateEvent.SqlState);
        Assert.Equal("IX_ActivityAggregates_FocusSessionId_ClientEventId", duplicateEvent.ConstraintName);

        var duplicatePause = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SessionPauses" ("Id","FocusSessionId","StartedAt")
            VALUES ({Guid.NewGuid()}, {session}, {now})
            """));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicatePause.SqlState);
        var reversedPause = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SessionPauses" ("Id","FocusSessionId","StartedAt","EndedAt")
            VALUES ({Guid.NewGuid()}, {session}, {now}, {now.AddSeconds(-1)})
            """));
        Assert.Equal(PostgresErrorCodes.CheckViolation, reversedPause.SqlState);
        Assert.Equal(1, await db.ActivityAggregates.CountAsync());
        Assert.Equal(1, await db.FocusStateRecords.CountAsync());
    }

    private static StringContent JsonContent(string body) => new(body, Encoding.UTF8, "application/json");
}
