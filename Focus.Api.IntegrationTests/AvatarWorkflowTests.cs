using System.IO.Pipes;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Focus.Agent.Sync;
using Focus.Avatar.Protocol;
using Focus.Engine;
using Xunit.Abstractions;
using Activity = Focus.Agent.Sync.Activity;
using static Focus.Api.IntegrationTests.SessionWorkflowTests;

namespace Focus.Api.IntegrationTests;

[Collection("Avatar desktop")]
public sealed class AvatarWorkflowTests(ITestOutputHelper output)
{
    [PostgresFact]
    public async Task BridgeUsesOwnedSessionAndPublishesPersistedEstimateAfterReconnect()
    {
        await using var api = new PostgresApi();
        await api.MigrateAsync();
        using var owner = api.CreateClient();
        using var agentHttp = api.CreateClient();
        using var otherHttp = api.CreateClient();
        using var connection = new AgentApi(agentHttp, api.Clock);
        using var otherAgent = new AgentApi(otherHttp, api.Clock);
        const string email = "avatar-owner@example.invalid";
        const string other = "avatar-other@example.invalid";
        await RegisterAsync(owner, email);
        await RegisterAsync(owner, other);
        Authorize(owner, await LoginAsync(owner, email));
        await connection.SignInAsync(email, StubPassword, default);
        await otherAgent.SignInAsync(other, StubPassword, default);
        await ExpectAsync(HttpStatusCode.OK, owner.PatchAsJsonAsync("/api/v1/settings/privacy", new { telemetryEnabled = true }));
        await ExpectAsync(HttpStatusCode.OK, owner.PutAsJsonAsync("/api/v1/settings/avatar",
            new { visibility = "AlwaysVisible", intensity = "Active", avatarId = (string?)null }));
        var profile = await JsonAsync(await ExpectAsync(HttpStatusCode.Created,
            owner.PostAsJsonAsync("/api/v1/settings/work-profiles", new
            {
                name = "Reading", focusMinutes = 10, breakMinutes = 3,
                rules = new[] { new { processName = "editor", category = "Allowed" } }
            })));
        var created = await JsonAsync(await ExpectAsync(HttpStatusCode.Created, owner.PostAsJsonAsync(Sessions,
            new { plannedDurationSeconds = 60, workProfileId = profile.GetProperty("id").GetGuid() })));
        var session = created.GetProperty("id").GetGuid();
        var path = $"{Sessions}/{session:D}";
        await ExpectAsync(HttpStatusCode.Created, owner.PostAsJsonAsync(path + "/goals", new { description = "Review paper" }));
        await RunBridgeAsync(session, otherAgent, async () =>
        {
            Assert.Equal("missing", (await ExchangeAsync(session, "snapshot")).result);
            Assert.Equal("missing", (await ExchangeAsync(session, "start")).result);
        });

        api.Clock.Advance(TimeSpan.FromSeconds(1));
        await RunBridgeAsync(session, connection, async () =>
        {
            var planned = await ExchangeAsync(session, "snapshot");
            Assert.Equal("Planned", planned.status);
            Assert.True(planned.hasTiming);
            Assert.Equal(0, planned.activeSeconds);
            Assert.Equal(60, planned.plannedSeconds);
            Assert.Equal("Review paper", planned.goal);
            Assert.Equal("AlwaysVisible", planned.visibility);
            Assert.Equal("Active", planned.intensity);
            Assert.Equal("rejected", (await ExchangeAsync(session, "complete")).result);
            Assert.Equal("accepted", (await ExchangeAsync(session, "start")).result);
            Assert.Equal("conflict", (await ExchangeAsync(session, "start")).result);
            Assert.Equal("Active", (await ExchangeAsync(session, "snapshot")).status);
        });

        await Assert.ThrowsAsync<TimeoutException>(() => ExchangeAsync(session, "snapshot", 150));
        var startedAt = (await owner.GetFromJsonAsync<JsonElement>(path)).GetProperty("startedAt").GetDateTimeOffset();
        api.Clock.Advance(TimeSpan.FromSeconds(15));
        Assert.Equal(new UploadReceipt(1, 0), await connection.UploadAsync(session,
            [new Activity(Guid.NewGuid(), startedAt, startedAt.AddSeconds(15), "editor", 2, 1, 0, 0)], default));
        var persisted = (await owner.GetFromJsonAsync<JsonElement>(path + "/focus-states"))
            .GetProperty("items")[0];

        await RunBridgeAsync(session, connection, async () =>
        {
            var active = await ExchangeAsync(session, "snapshot");
            Assert.Equal(persisted.GetProperty("estimatedState").GetString(), active.estimate);
            Assert.Equal(FocusClassifier.Version, active.ruleVersion);
            Assert.Equal("Focused", active.estimate);
            Assert.Equal(15, active.activeSeconds, 3);

            if (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("FOCUS_AVATAR_WINDOW_SMOKE") is { Length: > 0 } script)
                await VerifyPlayerWindowAsync(script, session);

            Assert.Equal("accepted", (await ExchangeAsync(session, "pause")).result);
            var paused = await ExchangeAsync(session, "snapshot");
            Assert.Equal("Paused", paused.status);
            Assert.Empty(paused.estimate);
            api.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.Equal(paused.activeSeconds, (await ExchangeAsync(session, "snapshot")).activeSeconds);
            Assert.Equal("accepted", (await ExchangeAsync(session, "resume")).result);
            Assert.Equal("Active", (await ExchangeAsync(session, "snapshot")).status);

            api.Clock.Advance(TimeSpan.FromSeconds(46));
            Assert.Empty((await ExchangeAsync(session, "snapshot")).estimate);
            await ExpectAsync(HttpStatusCode.OK, owner.PatchAsJsonAsync("/api/v1/settings/privacy", new { telemetryEnabled = false }));
            var withdrawn = await ExchangeAsync(session, "snapshot");
            Assert.False(withdrawn.telemetryAllowed);
            Assert.Empty(withdrawn.estimate);
            await ExpectAsync(HttpStatusCode.OK, owner.PostAsJsonAsync(path + "/complete",
                new { selfReportedConcentration = 4, selfReportedProductivity = 3 }));
            Assert.Equal("Completed", (await ExchangeAsync(session, "snapshot")).status);
        });
    }

    private static async Task RunBridgeAsync(Guid session, AgentApi agent, Func<Task> check)
    {
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var bridge = new AvatarBridge(session, agent).RunAsync(lifetime.Token);
        try { await check(); }
        finally
        {
            lifetime.Cancel();
            try { await bridge; }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }

    private async Task VerifyPlayerWindowAsync(string script, Guid session)
    {
        var scales = Environment.GetEnvironmentVariable("FOCUS_AVATAR_WINDOW_SCALES") ?? "100";
        foreach (var scale in scales.Split(','))
        {
            var suffix = scale == "100" ? "" : "-" + scale;
            output.WriteLine(await RunPlayerWindowAsync(script, session, TimeSpan.FromSeconds(26),
                "-ClientPercent", scale, "-Screenshot", $"artifacts/avatar-window{suffix}.png"));
        }
    }

    internal static async Task<string> RunPlayerWindowAsync(string script, Guid session, TimeSpan timeout,
        params string[] options)
    {
        var info = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in new[] { "-NoProfile", "-File", script, "-SessionId", session.ToString("D") }.Concat(options))
            info.ArgumentList.Add(argument);
        info.WorkingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(script))!;
        using var smoke = Process.Start(info)!;
        using var deadline = new CancellationTokenSource(timeout);
        var output = smoke.StandardOutput.ReadToEndAsync(deadline.Token);
        var errors = smoke.StandardError.ReadToEndAsync(deadline.Token);
        await smoke.WaitForExitAsync(deadline.Token);
        var transcript = await output + await errors;
        Assert.True(smoke.ExitCode == 0, transcript);
        Assert.Contains("PASS:", transcript);
        return transcript;
    }

    private static async Task<AvatarMessage> ExchangeAsync(Guid session, string verb, int connectTimeout = 2000)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var pipe = new NamedPipeClientStream(".", AvatarProtocol.PipeName(session), PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(connectTimeout, deadline.Token);
        await AvatarProtocol.WriteAsync(pipe, Encoding.ASCII.GetBytes(verb), deadline.Token);
        var bytes = await AvatarProtocol.ReadAsync(pipe, deadline.Token);
        await pipe.WriteAsync(new byte[] { 1 }, deadline.Token);
        var data = JsonSerializer.Deserialize<AvatarMessage>(bytes, new JsonSerializerOptions { IncludeFields = true })!;
        Assert.True(data.IsValid(session));
        using var fields = JsonDocument.Parse(bytes);
        Assert.All(fields.RootElement.EnumerateObject(), field =>
            Assert.Contains(field.Name, new[] { "version", "sessionId", "result", "status", "estimate",
                "ruleVersion", "goal", "visibility", "intensity", "telemetryAllowed", "hasTiming", "activeSeconds", "plannedSeconds" }));
        return data;
    }
}
