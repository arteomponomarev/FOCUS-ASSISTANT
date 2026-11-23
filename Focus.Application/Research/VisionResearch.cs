using Focus.Domain.Entities;
using Focus.Engine;

namespace Focus.Application.Research;

public sealed record VisionComparison(FocusEstimate Baseline, FocusEstimate Experimental);
public sealed record VisionReport(long ObservedMs, long PresentMs, long NotDetectedMs, long UnknownMs,
    long FacingMs, long AwayMs, long ValidSamples, bool ExperimentalEnabled, VisionComparison? Comparison);

public sealed class VisionResearch(IResearchStore store, FocusClassifier classifier)
{
    public async Task<VisionReport> ReadAsync(Guid owner, Guid session, CancellationToken ct)
    {
        long present = 0, notDetected = 0, unknown = 0, facing = 0, away = 0, count = 0;
        await foreach (var row in store.ReadVisionAsync(owner, session, ct).WithCancellation(ct))
        {
            var vision = row.Vision!;
            present += vision.PresentMs;
            notDetected += vision.NotDetectedMs;
            unknown += vision.UnknownMs;
            facing += vision.FacingMs;
            away += vision.AwayMs;
            count += vision.ValidSamples;
        }
        var baseline = await store.GetLatestEstimateAsync(owner, session, ct);
        var comparison = baseline is null ? null : await CompareAsync(owner, baseline, ct);
        return new VisionReport(present + notDetected, present, notDetected, unknown, facing, away, count,
            await store.UsesExperimentalVisionAsync(owner, ct), comparison);
    }

    public async Task<VisionComparison?> CompareAsync(Guid owner, FocusStateRecord baseline, CancellationToken ct,
        DateTimeOffset? notBefore = null)
    {
        if (baseline.ActivityAggregateId is not { } id || baseline.RuleVersion != FocusClassifier.Version)
            return null;

        var camera = await store.FindVisionAsync(owner, baseline.FocusSessionId, baseline.StartedAt, baseline.EndedAt, ct);
        if (camera is null || (notBefore.HasValue && camera.StartedAt < notBefore))
            return null;

        var activity = await store.FindActivityAsync(owner, baseline.FocusSessionId, id, ct);
        if (activity is null)
            return null;

        var start = camera.StartedAt > baseline.StartedAt ? camera.StartedAt : baseline.StartedAt;
        var end = camera.EndedAt < baseline.EndedAt ? camera.EndedAt : baseline.EndedAt;
        var overlap = (end - start).TotalMilliseconds /
            Math.Max((baseline.EndedAt - baseline.StartedAt).TotalMilliseconds, (camera.EndedAt - camera.StartedAt).TotalMilliseconds);
        var original = new FocusEstimate(baseline.State, baseline.RuleVersion, baseline.Reason ?? "");
        return new VisionComparison(original, classifier.EvaluateWithVision(original, camera.Vision,
            activity.KeyboardEventCount == 0 && activity.MouseClickCount == 0 && activity.WindowSwitchCount == 0, overlap));
    }
}
