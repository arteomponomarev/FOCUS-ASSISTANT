namespace Focus.Agent.Models
{
    internal sealed class TelemetrySnapshot
    {
        public DateTimeOffset StartedAt { get; init; }

        public DateTimeOffset EndedAt { get; init; }

        public double IntervalDurationSeconds { get; init; }

        public int? DominantProcessId { get; init; }

        public string? DominantProcessName { get; init; }

        public double IdleDurationSeconds { get; init; }

        public double IdleRatio { get; init; }

        public int KeyboardEvents { get; init; }

        public int MouseEvents { get; init; }

        public int WindowSwitches { get; init; }

        public IReadOnlyList<ApplicationUsageInfo> ApplicationUsage { get; init; }
            = [];
    }
}
