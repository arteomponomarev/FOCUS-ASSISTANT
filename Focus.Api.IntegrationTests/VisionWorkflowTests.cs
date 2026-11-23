using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Focus.Agent.Sync;
using Focus.Domain.Entities;
using Focus.Engine;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Focus.Api.IntegrationTests.SessionWorkflowTests;

namespace Focus.Api.IntegrationTests;

[Collection("Avatar desktop")]
public sealed class VisionWorkflowTests
{
    private const string Privacy = "/api/v1/settings/privacy";

    [PostgresFact]
    public async Task CameraConsentIsIndependentAndVisionRemainsCoarseAndIdempotent()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync("20261122123708_TrackLoginFamily");
        using var owner = api.CreateClient();
        using var agentHttp = api.CreateClient();
        using var stranger = api.CreateClient();
        const string email = "camera-owner@example.invalid";
        var legacyId = Guid.NewGuid();
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FocusDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Users" ("Id", "Email", "DisplayName", "CreatedAt")
                VALUES ({legacyId}, {"legacy-camera@example.invalid"}, {"Legacy account"}, {api.Clock.GetUtcNow()})
                """);
        }
        await api.MigrateAsync();
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FocusDbContext>();
            var legacy = await db.Users.SingleAsync(x => x.Id == legacyId);
            Assert.Null(legacy.CameraConsentAt);
            Assert.False(legacy.VisionExperimental);
        }
        await RegisterAsync(owner, email);
        await RegisterAsync(stranger, "camera-other@example.invalid");
        Authorize(owner, await LoginAsync(owner, email));
        Authorize(stranger, await LoginAsync(stranger, "camera-other@example.invalid"));
        var initial = await owner.GetFromJsonAsync<JsonElement>(Privacy);
        Assert.False(initial.GetProperty("cameraEnabled").GetBoolean());
        Assert.False(initial.GetProperty("telemetryEnabled").GetBoolean());

        using var agent = new AgentApi(agentHttp, api.Clock) { CameraStatus = () => "Processing" };
        await agent.SignInAsync(email, StubPassword, default);
        var created = await JsonAsync(await ExpectAsync(HttpStatusCode.Created,
            owner.PostAsJsonAsync(Sessions, new { plannedDurationSeconds = 20 })));
        var session = created.GetProperty("id").GetGuid();
        var path = Sessions + "/" + session;
        await ExpectAsync(HttpStatusCode.OK, owner.PatchAsJsonAsync(Privacy, new { cameraEnabled = true }));
        var started = await JsonAsync(await ExpectAsync(HttpStatusCode.OK, owner.PostAsync(path + "/start", null)));
        var start = started.GetProperty("startedAt").GetDateTimeOffset();
        api.Clock.Advance(TimeSpan.FromSeconds(15));
        var primary = new Activity(Guid.NewGuid(), start, start.AddSeconds(15), null, 0, 0, 0, 0,
            new VisionAggregate(0, 14_000, 1000, 0, 0, 60));
        var keyboardOnly = new Activity(Guid.NewGuid(), start, start.AddSeconds(15), null, 0, 0, 0, 15);
        var prohibited = new
        {
            primary.ClientEventId,
            primary.StartedAt,
            primary.EndedAt,
            primary.KeyboardEventCount,
            primary.MouseClickCount,
            primary.WindowSwitchCount,
            primary.IdleSeconds,
            vision = new
            {
                presentMs = 0,
                notDetectedMs = 14_000,
                unknownMs = 1000,
                facingMs = 0,
                awayMs = 0,
                validSamples = 60,
                landmarks = new[] { 1, 2 }
            }
        };
        await ExpectAsync(HttpStatusCode.BadRequest, owner.PostAsJsonAsync(path + "/activity/batch", new { items = new[] { prohibited } }));
        Assert.Equal(new UploadReceipt(1, 0), await agent.UploadAsync(session, [primary], default));
        Assert.Equal(new UploadReceipt(0, 1), await agent.UploadAsync(session, [primary], default));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Assert.ThrowsAsync<RemoteFailure>(() => agent.UploadAsync(session, [keyboardOnly], default))).Status);
        await ExpectAsync(HttpStatusCode.NotFound, stranger.GetAsync(path + "/vision"));
        using var otherHttp = api.CreateClient();
        using var otherAgent = new AgentApi(otherHttp, api.Clock);
        await otherAgent.SignInAsync("camera-other@example.invalid", StubPassword, default);
        Assert.Equal(HttpStatusCode.NotFound,
            (await Assert.ThrowsAsync<RemoteFailure>(() => otherAgent.UploadAsync(session, [primary], default))).Status);
        Assert.Equal(0, (await owner.GetFromJsonAsync<JsonElement>(path + "/activity")).GetProperty("totalCount").GetInt64());

        // Upload late desktop data only after obtaining its independent consent; use an overlapping future window.
        await ExpectAsync(HttpStatusCode.OK, owner.PatchAsJsonAsync(Privacy,
            new { telemetryEnabled = true, visionExperimental = true }));
        var next = start.AddSeconds(15);
        api.Clock.Advance(TimeSpan.FromSeconds(15));
        var second = primary with { ClientEventId = Guid.NewGuid(), StartedAt = next, EndedAt = next.AddSeconds(15) };
        var baseline = keyboardOnly with { StartedAt = next, EndedAt = next.AddSeconds(15) };
        Assert.Equal(new UploadReceipt(2, 0), await agent.UploadAsync(session, [second, baseline], default));
        var avatar = await agent.ReadAvatarAsync(session, default);
        Assert.Equal("Absent", avatar.estimate);
        Assert.Equal("Processing", avatar.cameraState);
        Assert.Equal(FocusClassifier.VisionVersion, avatar.ruleVersion);
        if (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("FOCUS_VISION_WINDOW_SMOKE") is { Length: > 0 } script)
            await VerifyPlayerAsync(agent, session, script);
        var stored = await owner.GetFromJsonAsync<JsonElement>(path + "/focus-states");
        Assert.Equal("Idle", stored.GetProperty("items")[0].GetProperty("estimatedState").GetString());

        await ExpectAsync(HttpStatusCode.OK, owner.PostAsync(path + "/pause", null));
        api.Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Assert.ThrowsAsync<RemoteFailure>(() =>
            agent.UploadAsync(session, [second with { ClientEventId = Guid.NewGuid(), StartedAt = next.AddSeconds(15),
                EndedAt = next.AddSeconds(16), Vision = new VisionAggregate(1000, 0, 0, 0, 0, 5) }], default))).Status);
        await ExpectAsync(HttpStatusCode.OK, owner.PostAsync(path + "/resume", null));
        var resume = api.Clock.GetUtcNow();
        api.Clock.Advance(TimeSpan.FromSeconds(5));
        await ExpectAsync(HttpStatusCode.OK, owner.PostAsJsonAsync(path + "/complete",
            new { selfReportedConcentration = 4, selfReportedProductivity = 4 }));
        var tail = second with
        {
            ClientEventId = Guid.NewGuid(),
            StartedAt = resume,
            EndedAt = resume.AddSeconds(5),
            Vision = new VisionAggregate(4000, 0, 1000, 2000, 1000, 18)
        };
        Assert.Equal(new UploadReceipt(1, 0), await agent.UploadAsync(session, [tail], default));
        var report = await owner.GetFromJsonAsync<JsonElement>(path + "/vision");
        Assert.Equal(32_000, report.GetProperty("observedMs").GetInt64());
        Assert.Equal(3000, report.GetProperty("unknownMs").GetInt64());
        Assert.Equal(2000, report.GetProperty("facingMs").GetInt64());
        Assert.Equal("Idle", report.GetProperty("comparison").GetProperty("baseline").GetProperty("state").GetString());

        await ExpectAsync(HttpStatusCode.NoContent, owner.PostAsync("/api/v1/auth/logout?stopCamera=true", null));
        Assert.Equal("Disabled", (await agent.ReadAvatarAsync(session, default)).cameraState);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Assert.ThrowsAsync<RemoteFailure>(() => agent.UploadAsync(session, [tail], default))).Status);
    }

    private static async Task VerifyPlayerAsync(AgentApi agent, Guid session, string script)
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(38));
        var bridge = new AvatarBridge(session, agent).RunAsync(stop.Token);
        try
        {
            await AvatarWorkflowTests.RunPlayerWindowAsync(script, session, TimeSpan.FromSeconds(28),
                "-Screenshot", "artifacts/vision-avatar-player.png", "-CaptureOnly");
        }
        finally
        {
            await stop.CancelAsync();
            try { await bridge; }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        }
    }
}
