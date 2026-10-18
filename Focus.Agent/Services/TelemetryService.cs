using Focus.Agent.Collectors;
using Focus.Agent.Models;

namespace Focus.Agent.Services
{
    internal sealed class TelemetryService
    {
        private readonly ActiveWindowCollector _activeWindowCollector;
        private readonly IdleTimeCollector _idleTimeCollector;
        private readonly InputActivityCollector _inputActivityCollector;
        private readonly WindowSwitchTracker _windowSwitchTracker;

        public TelemetryService(
            ActiveWindowCollector activeWindowCollector,
            IdleTimeCollector idleTimeCollector,
            InputActivityCollector inputActivityCollector,
            WindowSwitchTracker windowSwitchTracker)
        {
            _activeWindowCollector = activeWindowCollector;
            _idleTimeCollector = idleTimeCollector;
            _inputActivityCollector = inputActivityCollector;
            _windowSwitchTracker = windowSwitchTracker;
        }

        public async Task<TelemetrySnapshot> CollectIntervalAsync(
            TimeSpan interval,
            TimeSpan samplingInterval,
            TimeSpan idleThreshold,
            CancellationToken cancellationToken = default)
        {
            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            DateTimeOffset finishAt = startedAt.Add(interval);

            var usage = new Dictionary<int, UsageAccumulator>();

            double idleDurationSeconds = 0;

            ActiveWindowInfo? currentWindow =
                _activeWindowCollector.Collect();

            _windowSwitchTracker.Observe(currentWindow);

            bool isIdle =
                _idleTimeCollector.Collect() >= idleThreshold;

            DateTimeOffset previousSampleAt = startedAt;

            while (DateTimeOffset.UtcNow < finishAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                TimeSpan remaining =
                    finishAt - DateTimeOffset.UtcNow;

                TimeSpan delay =
                    remaining < samplingInterval
                        ? remaining
                        : samplingInterval;

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(
                        delay,
                        cancellationToken);
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;

                double elapsedSeconds =
                    (now - previousSampleAt).TotalSeconds;

                AddApplicationUsage(
                    usage,
                    currentWindow,
                    elapsedSeconds);

                if (isIdle)
                {
                    idleDurationSeconds += elapsedSeconds;
                }

                currentWindow =
                    _activeWindowCollector.Collect();

                _windowSwitchTracker.Observe(currentWindow);

                isIdle =
                    _idleTimeCollector.Collect() >= idleThreshold;

                previousSampleAt = now;
            }

            DateTimeOffset endedAt = DateTimeOffset.UtcNow;

            double intervalDurationSeconds =
                (endedAt - startedAt).TotalSeconds;

            InputActivitySnapshot inputActivity =
                _inputActivityCollector.SnapshotAndReset();

            int windowSwitches =
                _windowSwitchTracker.SnapshotAndReset();

            List<ApplicationUsageInfo> applicationUsage =
                usage.Values
                    .OrderByDescending(x => x.DurationSeconds)
                    .Select(x => new ApplicationUsageInfo
                    {
                        ProcessId = x.ProcessId,
                        ProcessName = x.ProcessName,
                        WindowTitle = x.WindowTitle,
                        DurationSeconds = x.DurationSeconds
                    })
                    .ToList();

            ApplicationUsageInfo? dominantApplication =
                applicationUsage.FirstOrDefault();

            return new TelemetrySnapshot
            {
                StartedAt = startedAt,
                EndedAt = endedAt,

                IntervalDurationSeconds =
                    intervalDurationSeconds,

                DominantProcessId =
                    dominantApplication?.ProcessId,

                DominantProcessName =
                    dominantApplication?.ProcessName,

                DominantWindowTitle =
                    dominantApplication?.WindowTitle,

                IdleDurationSeconds =
                    idleDurationSeconds,

                IdleRatio =
                    intervalDurationSeconds > 0
                        ? idleDurationSeconds /
                          intervalDurationSeconds
                        : 0,

                KeyboardEvents =
                    inputActivity.KeyboardEvents,

                MouseEvents =
                    inputActivity.MouseEvents,

                WindowSwitches =
                    windowSwitches,

                ApplicationUsage =
                    applicationUsage
            };
        }

        private static void AddApplicationUsage(
            Dictionary<int, UsageAccumulator> usage,
            ActiveWindowInfo? window,
            double durationSeconds)
        {
            if (window == null || durationSeconds <= 0)
            {
                return;
            }

            if (!usage.TryGetValue(
                    window.ProcessId,
                    out UsageAccumulator? accumulator))
            {
                accumulator = new UsageAccumulator
                {
                    ProcessId = window.ProcessId,
                    ProcessName = window.ProcessName
                };

                usage.Add(
                    window.ProcessId,
                    accumulator);
            }

            accumulator.WindowTitle = window.WindowTitle;

            accumulator.DurationSeconds +=
                durationSeconds;
        }

        private sealed class UsageAccumulator
        {
            public int ProcessId { get; init; }

            public string ProcessName { get; init; } = "";

            public string WindowTitle { get; set; } = "";

            public double DurationSeconds { get; set; }
        }
    }
}