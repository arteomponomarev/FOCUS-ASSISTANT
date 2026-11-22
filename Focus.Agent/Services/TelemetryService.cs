using Focus.Agent.Collectors;
using Focus.Agent.Models;
using Focus.Agent.Sync;
using Focus.Domain.Entities;

namespace Focus.Agent.Services;

internal sealed class TelemetryService(
    ActiveWindowCollector windows,
    IdleTimeCollector idle,
    InputActivityCollector input,
    WindowSwitchTracker switches,
    TimeProvider clock) : ISignalSource
{
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromSeconds(5);
    private DateTimeOffset? _previousAt;
    private ActiveWindowInfo? _previousWindow;
    private bool _wasIdle;

    public void Start()
    {
        if (_previousAt is not null)
            return;

        switches.Reset();
        _previousWindow = windows.Collect();
        switches.Observe(_previousWindow);
        _wasIdle = idle.Collect() >= IdleThreshold;
        input.Start();
        input.SnapshotAndReset();
        _previousAt = clock.GetUtcNow();
    }

    public SignalSample? Sample()
    {
        if (_previousAt is not { } start)
            return null;

        var window = windows.Collect();
        switches.Observe(window);
        var nextIdle = idle.Collect() >= IdleThreshold;
        var counts = input.SnapshotAndReset();
        var switchCount = switches.SnapshotAndReset();
        var end = clock.GetUtcNow();
        var process = ProcessIdentity.TryNormalize(_previousWindow?.ProcessName, out var normalized)
            ? normalized : null;
        var sample = new SignalSample(start, end, process,
            counts.KeyboardEvents, counts.MouseEvents, switchCount,
            _wasIdle ? (end - start).TotalSeconds : 0);
        _previousAt = end;
        _previousWindow = window;
        _wasIdle = nextIdle;
        return sample;
    }

    public void Stop()
    {
        _previousAt = null;
        _previousWindow = null;
        input.Stop();
        input.SnapshotAndReset();
        switches.Reset();
    }
}
