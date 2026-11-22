using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Focus.Api.IntegrationTests.SessionWorkflowTests;

namespace Focus.Api.IntegrationTests;

public sealed class ResearchWorkflowTests
{
    [PostgresFact]
    public async Task PersistsObservedAndEstimatedDataAtomicallyAndKeepsResearchOwnerScoped()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync();
        using var owner = api.CreateClient();
        using var stranger = api.CreateClient();
        await RegisterAsync(owner, "research@example.invalid");
        await RegisterAsync(stranger, "outsider@example.invalid");
        Authorize(owner, await LoginAsync(owner, "research@example.invalid"));
        Authorize(stranger, await LoginAsync(stranger, "outsider@example.invalid"));

        var settings = "/api/v1/settings";
        var profile = new
        {
            name = "Writing",
            description = "Draft the methods chapter",
            focusMinutes = 40,
            breakMinutes = 10,
            rules = new[] { new { processName = "Editor", category = "Allowed" } }
        };
        var profileId = (await JsonAsync(await ExpectAsync(HttpStatusCode.Created,
            owner.PostAsJsonAsync(settings + "/work-profiles", profile)))).GetProperty("id").GetGuid();
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(settings + "/work-profiles/" + profileId));
        await ExpectAsync(HttpStatusCode.BadRequest, owner.PutAsJsonAsync(settings + "/work-profiles/" + profileId,
            profile with { rules = [new { processName = "editor", category = "Allowed" }, new { processName = "EDITOR", category = "Distracting" }] }));
        await ExpectAsync(HttpStatusCode.OK, owner.PutAsJsonAsync(settings + "/work-profiles/" + profileId, profile));

        var pomodoro = new { focusMinutes = 40, shortBreakMinutes = 10, longBreakMinutes = 25, intervalsBeforeLongBreak = 3 };
        await ExpectAsync(HttpStatusCode.OK, owner.PutAsJsonAsync(settings + "/pomodoro", pomodoro));
        await ExpectAsync(HttpStatusCode.OK, owner.PutAsJsonAsync(settings + "/avatar",
            new { visibility = "NotificationsOnly", intensity = "Quiet", avatarId = "assistant" }));
        Assert.Equal(40, (await owner.GetFromJsonAsync<JsonElement>(settings + "/pomodoro")).GetProperty("focusMinutes").GetInt32());
        Assert.Equal(25, (await stranger.GetFromJsonAsync<JsonElement>(settings + "/pomodoro")).GetProperty("focusMinutes").GetInt32());

        var consentPath = settings + "/privacy";
        await ExpectAsync(HttpStatusCode.OK, owner.PatchAsJsonAsync(consentPath, new { telemetryEnabled = true }));
        await ExpectAsync(HttpStatusCode.NotFound, stranger.PostAsJsonAsync(Sessions,
            new { plannedDurationSeconds = 600, workProfileId = profileId }));
        var sessionId = (await JsonAsync(await ExpectAsync(HttpStatusCode.Created,
            owner.PostAsJsonAsync(Sessions, new { plannedDurationSeconds = 1200, workProfileId = profileId, goals = new[] { "Methods" } }))))
            .GetProperty("id").GetGuid();
        var path = Sessions + "/" + sessionId;
        await ExpectAsync(HttpStatusCode.Conflict, owner.DeleteAsync(settings + "/work-profiles/" + profileId));
        api.Clock.Advance(TimeSpan.FromSeconds(5));
        var started = (await JsonAsync(await ExpectAsync(HttpStatusCode.OK, owner.PostAsync(path + "/start", null))))
            .GetProperty("startedAt").GetDateTimeOffset();
        var start = started.AddSeconds(1);
        api.Clock.Advance(TimeSpan.FromSeconds(90));

        var aggregate = new
        {
            clientEventId = Guid.NewGuid(),
            startedAt = start,
            endedAt = start.AddSeconds(60),
            activeProcessName = "Editor",
            keyboardEventCount = 22,
            mouseClickCount = 4,
            windowSwitchCount = 1,
            idleSeconds = 6
        };
        var batch = path + "/activity/batch";
        await ExpectAsync(HttpStatusCode.NotFound, stranger.PostAsJsonAsync(batch, new { items = new[] { aggregate } }));
        await ExpectAsync(HttpStatusCode.BadRequest, owner.PostAsJsonAsync(batch, new { items = new[] { aggregate with { mouseClickCount = -1 } } }));
        await ExpectAsync(HttpStatusCode.BadRequest, owner.PostAsJsonAsync(batch, new { items = Enumerable.Repeat(aggregate, 101) }));
        await ExpectAsync(HttpStatusCode.BadRequest, owner.PostAsJsonAsync(batch, new
        {
            items = new[] { new { aggregate.clientEventId, aggregate.startedAt, aggregate.endedAt, windowTitle = "Private" } }
        }));
        await ExpectAsync(HttpStatusCode.UnprocessableEntity, owner.PostAsJsonAsync(batch, new
        {
            items = new[] { aggregate with { startedAt = start.AddSeconds(-10) } }
        }));
        var invalidSecond = aggregate with
        {
            clientEventId = Guid.NewGuid(),
            startedAt = start.AddSeconds(61),
            endedAt = start.AddMinutes(4)
        };
        await ExpectAsync(HttpStatusCode.UnprocessableEntity,
            owner.PostAsJsonAsync(batch, new { items = new[] { aggregate, invalidSecond } }));
        Assert.Equal(0, (await owner.GetFromJsonAsync<JsonElement>(path + "/activity")).GetProperty("totalCount").GetInt64());

        var results = await Task.WhenAll(Enumerable.Range(0, 2)
            .Select(_ => owner.PostAsJsonAsync(batch, new { items = new[] { aggregate } })));
        Assert.All(results, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
        var counts = await Task.WhenAll(results.Select(JsonAsync));
        Assert.Single(counts, x => x.GetProperty("created").GetInt32() == 1);
        Assert.Single(counts, x => x.GetProperty("duplicates").GetInt32() == 1);
        await ExpectAsync(HttpStatusCode.Conflict, owner.PostAsJsonAsync(batch,
            new { items = new[] { aggregate with { keyboardEventCount = 23 } } }));
        await ExpectAsync(HttpStatusCode.Conflict, owner.PostAsJsonAsync(batch,
            new { items = new[] { aggregate with { clientEventId = Guid.NewGuid() } } }));

        await ExpectAsync(HttpStatusCode.OK, owner.PatchAsJsonAsync(consentPath, new { telemetryEnabled = false }));
        await ExpectAsync(HttpStatusCode.Forbidden, owner.PostAsJsonAsync(batch, new { items = new[] { aggregate } }));
        await ExpectAsync(HttpStatusCode.OK, owner.PostAsJsonAsync(path + "/complete",
            new { selfReportedConcentration = 4, selfReportedProductivity = 2 }));

        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FocusDbContext>();
        var stored = await db.ActivityAggregates.AsNoTracking().SingleAsync();
        var derived = await db.FocusStateRecords.AsNoTracking().SingleAsync();
        Assert.Equal(stored.Id, derived.ActivityAggregateId);
        Assert.Equal("Focused", derived.State.ToString());
        var timeline = await owner.GetFromJsonAsync<JsonElement>(path + "/timeline");
        Assert.Equal(2, timeline.GetProperty("events").GetProperty("totalCount").GetInt64());
        Assert.Equal("Focused", (await owner.GetFromJsonAsync<JsonElement>(path + "/focus-states/current"))
            .GetProperty("latestEstimate").GetProperty("estimatedState").GetString());
        var summary = (await owner.GetFromJsonAsync<JsonElement>("/api/v1/analytics/summary")).GetProperty("metrics");
        Assert.Equal(1, summary.GetProperty("sessions").GetInt32());
        Assert.Equal(22, summary.GetProperty("keyboardEvents").GetInt64());
        Assert.Equal(60, summary.GetProperty("estimatedStateSeconds").GetProperty("Focused").GetDouble(), 4);
        Assert.Equal(4, summary.GetProperty("averageSelfReportedConcentration").GetDouble());
        Assert.Equal(2, summary.GetProperty("averageSelfReportedProductivity").GetDouble());
        var snapshot = await owner.GetFromJsonAsync<JsonElement>("/api/v1/analytics/research/sessions");
        Assert.Equal(1, snapshot.GetProperty("totalCount").GetInt64());
        Assert.Equal(0, (await stranger.GetFromJsonAsync<JsonElement>("/api/v1/analytics/summary")).GetProperty("metrics")
            .GetProperty("sessions").GetInt32());
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(path + "/timeline"));
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(path + "/goals"));
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(path + "/activity"));
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(path + "/focus-states"));
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(path + "/focus-states/current"));
        Assert.Equal(0, (await stranger.GetFromJsonAsync<JsonElement>("/api/v1/analytics/research/sessions"))
            .GetProperty("totalCount").GetInt64());
        Assert.Equal("outsider@example.invalid", (await stranger.GetFromJsonAsync<JsonElement>(consentPath + "/export"))
            .GetProperty("profile").GetProperty("email").GetString());

        await ExpectAsync(HttpStatusCode.OK, owner.GetAsync(consentPath + "/export"));
        await ExpectAsync(HttpStatusCode.Unauthorized, owner.PostAsJsonAsync(consentPath + "/delete-account",
            new { password = "wrong" }));
        await ExpectAsync(HttpStatusCode.NoContent, owner.PostAsJsonAsync(consentPath + "/delete-account",
            new { password = StubPassword }));
        await ExpectAsync(HttpStatusCode.Unauthorized, owner.GetAsync(Sessions));
        Assert.False(await db.ActivityAggregates.AnyAsync());
        Assert.False(await db.FocusStateRecords.AnyAsync());
        Assert.False(await db.FocusSessions.AnyAsync());
    }
}
