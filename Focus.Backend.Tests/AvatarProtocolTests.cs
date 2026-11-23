using Focus.Avatar.Protocol;

namespace Focus.Backend.Tests;

public sealed class AvatarProtocolTests
{
    [Theory]
    [InlineData("", AvatarMood.Neutral)]
    [InlineData("Neutral", AvatarMood.Neutral)]
    [InlineData("Focused", AvatarMood.Focused)]
    [InlineData("Distracted", AvatarMood.Distracted)]
    [InlineData("Idle", AvatarMood.Idle)]
    [InlineData("Break", AvatarMood.Break)]
    [InlineData("Absent", AvatarMood.Absent)]
    public void MapsKnownEstimatesToExpressions(string estimate, AvatarMood expected)
    {
        var snapshot = AvatarMessage.Reply(Guid.NewGuid(), "ok");
        snapshot.status = "Active";
        snapshot.estimate = estimate;
        Assert.Equal(expected, AvatarPresentation.Mood(snapshot, false));
        Assert.Equal(AvatarMood.Celebrating, AvatarPresentation.Mood(snapshot, true));
    }

    [Fact]
    public void OnlyActiveConsentedSnapshotsCanHaveAnEstimate()
    {
        var session = Guid.NewGuid();
        var snapshot = AvatarMessage.Reply(session, "ok");
        snapshot.status = "Active";
        snapshot.estimate = "Focused";
        snapshot.ruleVersion = "rules-1";
        Assert.False(snapshot.IsValid(session));
        snapshot.telemetryAllowed = true;
        Assert.True(snapshot.IsValid(session));
        Assert.False(snapshot.IsValid(Guid.NewGuid()));
        snapshot.status = "Paused";
        Assert.False(snapshot.IsValid(session));
        snapshot.estimate = "";
        Assert.True(snapshot.IsValid(session));
        snapshot.version++;
        Assert.False(snapshot.IsValid(session));
    }

    [Fact]
    public void HonorsVisibilityAndCelebratesOnlyObservedCompletion()
    {
        var snapshot = AvatarMessage.Reply(Guid.NewGuid(), "ok");
        snapshot.status = "Paused";
        Assert.Equal(AvatarMood.Break, AvatarPresentation.Mood(snapshot, false));
        Assert.True(AvatarPresentation.ShowCompanion(snapshot, false));
        snapshot.visibility = "Hidden";
        Assert.False(AvatarPresentation.ShowCompanion(snapshot, true));
        snapshot.visibility = "AlwaysVisible";
        Assert.True(AvatarPresentation.ShowCompanion(snapshot, false));
        snapshot.visibility = "NotificationsOnly";
        Assert.False(AvatarPresentation.ShowCompanion(snapshot, false));
        Assert.True(AvatarPresentation.ShowCompanion(snapshot, true));
        snapshot.intensity = "Quiet";
        Assert.False(AvatarPresentation.ShowCompanion(snapshot, true));
        Assert.True(AvatarPresentation.ShouldCelebrate("Active", "Completed"));
        Assert.True(AvatarPresentation.ShouldCelebrate("Paused", "Completed"));
        Assert.False(AvatarPresentation.ShouldCelebrate("", "Completed"));
        Assert.False(AvatarPresentation.ShouldCelebrate("Completed", "Completed"));
        Assert.False(AvatarPresentation.ShouldCelebrate("Active", "Cancelled"));
    }

    [Fact]
    public void TimerUsesServerActiveTimeAndStopsWhilePaused()
    {
        var session = Guid.NewGuid();
        var snapshot = AvatarMessage.Reply(session, "ok");
        snapshot.status = "Active";
        Assert.True(snapshot.IsValid(session)); // A v1 Agent can omit timing.
        snapshot.hasTiming = true;
        snapshot.activeSeconds = 42.5;
        snapshot.plannedSeconds = 60;
        Assert.True(snapshot.IsValid(session));
        Assert.Equal(43.5, AvatarPresentation.ActiveSeconds(snapshot, 1));
        Assert.Equal(45.5, AvatarPresentation.ActiveSeconds(snapshot, 10));
        snapshot.status = "Paused";
        Assert.Equal(42.5, AvatarPresentation.ActiveSeconds(snapshot, 100));
        snapshot.activeSeconds = double.NaN;
        Assert.False(snapshot.IsValid(session));
        snapshot.activeSeconds = 42.5;
        snapshot.plannedSeconds = 0;
        Assert.False(snapshot.IsValid(session));
    }

    [Fact]
    public void TiredIsAVisualResponseToContinuousIdleAndOfflineHasItsOwnPose()
    {
        var snapshot = AvatarMessage.Reply(Guid.NewGuid(), "ok");
        snapshot.status = "Active";
        snapshot.estimate = "Idle";
        Assert.Equal(AvatarMood.Idle, AvatarPresentation.Mood(snapshot, false, 29.9));
        Assert.Equal(AvatarMood.Tired, AvatarPresentation.Mood(snapshot, false, 30));
        snapshot.estimate = "Neutral";
        Assert.Equal(AvatarMood.Neutral, AvatarPresentation.Mood(snapshot, false, 60));
        snapshot.result = "offline";
        Assert.Equal(AvatarMood.Disconnected, AvatarPresentation.Mood(snapshot, false));
    }

    [Fact]
    public async Task ReadsAndWritesBoundedFrames()
    {
        using var stream = new MemoryStream();
        await AvatarProtocol.WriteAsync(stream, new byte[] { 87, 90, 81 }, CancellationToken.None);
        stream.Position = 0;
        Assert.Equal(new byte[] { 87, 90, 81 }, await AvatarProtocol.ReadAsync(stream, CancellationToken.None));
        using var oversized = new MemoryStream(new byte[] { 255, 255 });
        await Assert.ThrowsAsync<InvalidDataException>(() => AvatarProtocol.ReadAsync(oversized, CancellationToken.None));
        using var truncated = new MemoryStream(new byte[] { 0, 2, 87 });
        await Assert.ThrowsAsync<EndOfStreamException>(() => AvatarProtocol.ReadAsync(truncated, CancellationToken.None));
    }
}
