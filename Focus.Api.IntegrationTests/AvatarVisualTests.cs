using Focus.Agent.Sync;
using Focus.Avatar.Protocol;
using Xunit.Abstractions;

namespace Focus.Api.IntegrationTests;

public sealed class AvatarVisualFactAttribute : FactAttribute
{
    public AvatarVisualFactAttribute()
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FOCUS_AVATAR_VISUAL_SMOKE")))
            Skip = "Set FOCUS_AVATAR_VISUAL_SMOKE to the window test script on an interactive Windows desktop.";
    }
}

[Collection("Avatar desktop")]
public sealed class AvatarVisualTests(ITestOutputHelper output)
{
    [AvatarVisualFact]
    public async Task CapturesNineStatesFromTheBuiltPlayer()
    {
        var script = Environment.GetEnvironmentVariable("FOCUS_AVATAR_VISUAL_SMOKE")!;
        var repo = Path.GetDirectoryName(Path.GetDirectoryName(script))!;
        var directory = Path.Combine(repo, "artifacts", "avatar-state-captures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var session = Guid.NewGuid();
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(95));
        var bridge = new AvatarBridge(session, new VisualEndpoint(directory)).RunAsync(lifetime.Token);
        try
        {
            output.WriteLine(await AvatarWorkflowTests.RunPlayerWindowAsync(script, session, TimeSpan.FromSeconds(85),
                "-Screenshot", Path.Combine(directory, "window.png"), "-SequenceDirectory", directory));
            output.WriteLine(directory);
            foreach (var name in new[] { "Neutral", "Focused", "Distracted", "Break", "Absent", "Idle", "Tired",
                "Celebrating", "Disconnected" })
                Assert.True(new FileInfo(Path.Combine(directory, name + ".png")).Length > 1000);
        }
        finally
        {
            await lifetime.CancelAsync();
            try { await bridge; }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }

    private sealed class VisualEndpoint(string directory) : IAvatarEndpoint
    {
        public async Task<AvatarMessage> ReadAvatarAsync(Guid session, CancellationToken ct)
        {
            var cue = Path.Combine(directory, "cue.txt");
            var phase = File.Exists(cue) ? await File.ReadAllTextAsync(cue, ct) : "Neutral";
            await File.WriteAllTextAsync(Path.Combine(directory, "seen.txt"), phase, ct);
            if (phase == "Disconnected")
                return AvatarMessage.Reply(session, "offline");

            var reply = AvatarMessage.Reply(session, "ok");
            reply.status = phase == "Celebrating" ? "Completed" : phase == "Break" ? "Paused" : "Active";
            reply.estimate = reply.status == "Active" ? phase == "Tired" ? "Idle" : phase : "";
            reply.goal = "Review the research notes";
            reply.ruleVersion = reply.estimate.Length == 0 ? "" : "fixture";
            reply.intensity = "Active";
            reply.visibility = "AlwaysVisible";
            reply.telemetryAllowed = true;
            reply.hasTiming = true;
            reply.activeSeconds = 642;
            reply.plannedSeconds = 1500;
            return reply;
        }

        public Task TransitionAvatarAsync(Guid session, string action, CancellationToken ct) =>
            throw new InvalidOperationException("The visual fixture does not accept session commands.");
    }
}
