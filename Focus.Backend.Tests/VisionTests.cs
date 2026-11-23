using Focus.Agent.Sync;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Focus.Engine;

namespace Focus.Backend.Tests;

public sealed class VisionTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MissingFramesAndConflictingOrientationsAreNotExtrapolated()
    {
        var samples = new VisionSamples(TimeSpan.FromMilliseconds(300));
        samples.Add(Start, 'F');
        samples.Add(Start.AddMilliseconds(200), 'F');
        samples.Add(Start.AddMilliseconds(400), 'A');
        samples.Add(Start.AddMilliseconds(800), 'N');  // Gap above the admissible interval.
        samples.Add(Start.AddMilliseconds(1000), 'N');
        var first = samples.Take(Start, Start.AddMilliseconds(300));
        var second = samples.Take(Start.AddMilliseconds(300), Start.AddSeconds(1));
        Assert.Equal(new VisionSlice(TimeSpan.FromMilliseconds(300).Ticks, 0,
            TimeSpan.FromMilliseconds(200).Ticks, 0, 1), first);
        Assert.Equal(new VisionSlice(TimeSpan.FromMilliseconds(100).Ticks, TimeSpan.FromMilliseconds(200).Ticks,
            0, 0, 2), second);
    }

    [Fact]
    public void CameraCaptureFlushesTheConfirmedShortTailAndRejectsPausedAndLateSamples()
    {
        var saved = new List<PendingActivity>();
        var buffer = new CaptureBuffer(Guid.NewGuid(), saved.Add, camera: true);
        var privacy = new Permission(false, null, true, Start);
        for (var i = 0; i < 22; i++)
            buffer.Add(new SignalSample(Start.AddSeconds(i), Start.AddSeconds(i + 1),
                null, 0, 0, 0, 0, new VisionSlice(TimeSpan.TicksPerSecond, 0, 0, 0, 5)));
        buffer.Confirm(new Authority(privacy,
            new SessionWindow(SessionStatus.Completed, Start, Start.AddSeconds(20),
                [new PauseWindow(Start.AddSeconds(16), Start.AddSeconds(17))]), Start.AddSeconds(21)));
        Assert.Equal(new[] { 15000, 1000, 3000 }, saved.Select(entry => entry.Item.Vision!.PresentMs));
        Assert.All(saved, entry => Assert.True(entry.Item.Vision!.Fits(
            (long)(entry.Item.EndedAt - entry.Item.StartedAt).TotalMilliseconds)));
        Assert.Equal(Start.AddSeconds(20), saved[^1].Item.EndedAt);
        buffer.Confirm(new Authority(new Permission(true, Start), null, Start.AddSeconds(22)));
        Assert.Equal(3, saved.Count);
    }

    [Fact]
    public void CameraEvidenceChangesOnlyAnIdleSilentWindowWithSustainedNonDetection()
    {
        var classifier = new FocusClassifier();
        var signal = new SignalWindow(15, 0, 0, 0, 15, false, ProcessCategory.Neutral);
        var baseline = classifier.Evaluate(signal);
        var missing = new VisionAggregate(0, 14_000, 1000, 0, 0, 60);
        Assert.Equal(FocusStateType.Idle, baseline.State);
        Assert.Equal(baseline, classifier.EvaluateWithVision(baseline, null, true, 1));
        Assert.Equal(baseline, classifier.EvaluateWithVision(baseline, missing, true, .79));
        Assert.Equal(baseline, classifier.EvaluateWithVision(baseline, missing, false, 1));
        Assert.Equal(baseline, classifier.EvaluateWithVision(baseline,
            new VisionAggregate(8000, 0, 7000, 0, 8000, 25), true, 1));
        var result = classifier.EvaluateWithVision(baseline, missing, true, .9);
        Assert.Equal(FocusStateType.Absent, result.State);
        Assert.Equal(FocusClassifier.VisionVersion, result.RuleVersion);
    }
}
