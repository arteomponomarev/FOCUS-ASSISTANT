namespace Focus.Agent.Models
{
    internal sealed class ApplicationUsageInfo
    {
        public int ProcessId { get; init; }

        public string ProcessName { get; init; } = "";

        public double DurationSeconds { get; init; }
    }
}
