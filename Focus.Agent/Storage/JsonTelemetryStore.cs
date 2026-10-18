using Focus.Agent.Models;
using System.Text.Json;

namespace Focus.Agent.Storage
{
    internal sealed class JsonTelemetryStore
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        private readonly string _directoryPath;

        public JsonTelemetryStore()
        {
            _directoryPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "FocusAssistant",
                "telemetry");

            Directory.CreateDirectory(_directoryPath);
        }

        public async Task SaveAsync(
            TelemetrySnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            string fileName =
                $"telemetry-{DateTime.UtcNow:yyyy-MM-dd}.jsonl";

            string filePath =
                Path.Combine(_directoryPath, fileName);

            string json =
                JsonSerializer.Serialize(snapshot, JsonOptions);

            await File.AppendAllTextAsync(
                filePath,
                json + Environment.NewLine,
                cancellationToken);
        }

        public string DirectoryPath => _directoryPath;
    }
}