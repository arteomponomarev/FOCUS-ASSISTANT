using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Focus.Agent.Sync;
using Focus.Domain.Enums;
using static Focus.Api.IntegrationTests.SessionWorkflowTests;

namespace Focus.Api.IntegrationTests;

public sealed class AgentPipelineTests
{
    [PostgresFact]
    public async Task AgentDeliversAFinishedTwentySecondSessionThroughTheRealApi()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync();
        using var browser = api.CreateClient();
        using var agentHttp = api.CreateClient();
        const string email = "agent-pipeline@example.invalid";
        await RegisterAsync(browser, email);
        Authorize(browser, await LoginAsync(browser, email));
        await ExpectAsync(HttpStatusCode.OK, browser.PatchAsJsonAsync("/api/v1/settings/privacy",
            new { telemetryEnabled = true }));
        var created = await JsonAsync(await ExpectAsync(HttpStatusCode.Created,
            browser.PostAsJsonAsync(Sessions, new { plannedDurationSeconds = 20 })));
        var session = created.GetProperty("id").GetGuid();
        var path = Sessions + "/" + session;
        var dir = Directory.CreateTempSubdirectory("focus-agent-api-");

        try
        {
            var queued = new List<Activity>();
            using (var connection = new AgentApi(agentHttp, api.Clock))
            using (var outbox = new ActivityQueue(dir.FullName, x => x, x => x))
            {
                Assert.NotEqual(Guid.Empty, (await connection.SignInAsync(email, StubPassword, default)).Id);
                Assert.Equal(SessionStatus.Planned, (await connection.CheckAsync(session, default)).Session?.Status);

                api.Clock.Advance(TimeSpan.FromSeconds(1));
                var started = await JsonAsync(await ExpectAsync(HttpStatusCode.OK, browser.PostAsync(path + "/start", null)));
                var ticks = started.GetProperty("startedAt").GetDateTimeOffset().UtcTicks;
                var start = new DateTimeOffset(ticks - ticks % 10, TimeSpan.Zero);
                var buffer = new CaptureBuffer(session, entry =>
                {
                    queued.Add(entry.Item);
                    outbox.Add(entry);
                });
                AddSamples(buffer, start, 0, 15);
                api.Clock.Advance(TimeSpan.FromSeconds(15));
                var active = await connection.CheckAsync(session, default);
                buffer.Confirm(active);
                var delivery = new OfflineSync(connection, outbox, api.Clock);
                Assert.Equal(1, (await delivery.DeliverAsync(active.Privacy.ConsentedAt!.Value, default)).Accepted);

                AddSamples(buffer, start, 15, 22);
                api.Clock.Advance(TimeSpan.FromSeconds(5));
                await ExpectAsync(HttpStatusCode.OK, browser.PostAsJsonAsync(path + "/complete",
                    new { selfReportedConcentration = 4, selfReportedProductivity = 3 }));
                buffer.Confirm(await connection.CheckAsync(session, default));
                Assert.False(outbox.IsEmpty);
                Assert.Equal(2, queued.Count);
                Assert.Equal(5, (queued[1].EndedAt - queued[1].StartedAt).TotalSeconds, 3);
            }

            using var restartedHttp = api.CreateClient();
            using var restarted = new AgentApi(restartedHttp, api.Clock);
            using var recovered = new ActivityQueue(dir.FullName, x => x, x => x);
            await restarted.SignInAsync(email, StubPassword, default);
            var permission = (await restarted.CheckAsync(session, default)).Privacy;
            Assert.Equal(1, (await new OfflineSync(restarted, recovered, api.Clock)
                .DeliverAsync(permission.ConsentedAt!.Value, default)).Accepted);
            Assert.True(recovered.IsEmpty);

            var receipt = await restarted.UploadAsync(session, queued, default);
            Assert.Equal(new UploadReceipt(0, 2), receipt);
            var activity = await browser.GetFromJsonAsync<JsonElement>(path + "/activity");
            var estimates = await browser.GetFromJsonAsync<JsonElement>(path + "/focus-states");
            var ids = activity.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToHashSet();
            Assert.Equal(2, ids.Count);
            Assert.Equal(20, activity.GetProperty("items").EnumerateArray().Sum(x => x.GetProperty("keyboardEventCount").GetInt32()));
            Assert.Equal(2, estimates.GetProperty("totalCount").GetInt32());
            Assert.All(estimates.GetProperty("items").EnumerateArray(), item =>
            {
                Assert.Contains(item.GetProperty("activityAggregateId").GetGuid(), ids);
                Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("ruleVersion").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("reason").GetString()));
            });
            Assert.Equal(4, (await browser.GetFromJsonAsync<JsonElement>(path + "/timeline"))
                .GetProperty("events").GetProperty("totalCount").GetInt32());

            await ExpectAsync(HttpStatusCode.OK, browser.PatchAsJsonAsync("/api/v1/settings/privacy",
                new { telemetryEnabled = false }));
            Assert.False((await restarted.CheckAsync(session, default)).Privacy.TelemetryEnabled);
            var denied = await Assert.ThrowsAsync<RemoteFailure>(() => restarted.UploadAsync(session, queued, default));
            Assert.Equal(HttpStatusCode.Forbidden, denied.Status);

            await using var reopenedApi = api.Reopen();
            await api.DisposeAsync();
            using var reopenedBrowser = reopenedApi.CreateClient();
            Authorize(reopenedBrowser, await LoginAsync(reopenedBrowser, email));
            Assert.Equal(2, (await reopenedBrowser.GetFromJsonAsync<JsonElement>(path + "/activity"))
                .GetProperty("totalCount").GetInt32());
            Assert.Equal(2, (await reopenedBrowser.GetFromJsonAsync<JsonElement>(path + "/focus-states"))
                .GetProperty("totalCount").GetInt32());
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    private static void AddSamples(CaptureBuffer buffer, DateTimeOffset start, int from, int to)
    {
        for (var second = from; second < to; second++)
            buffer.Add(new SignalSample(start.AddSeconds(second), start.AddSeconds(second + 1), "editor", 1, 2, 1, 0));
    }
}
