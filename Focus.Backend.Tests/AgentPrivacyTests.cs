using System.Text.Json;
using Focus.Agent.Models;
using Focus.Agent.Storage;

namespace Focus.Backend.Tests;

public sealed class AgentPrivacyTests
{
    [Fact]
    public async Task JsonlContainsCountersAndProcessMetadataWithoutWindowTitles()
    {
        var fixture = Directory.CreateTempSubdirectory("focus-agent-privacy-");
        try
        {
            var store = new JsonTelemetryStore(fixture.FullName);
            await store.SaveAsync(new TelemetrySnapshot
            {
                KeyboardEvents = 7,
                MouseEvents = 3,
                DominantProcessName = "editor",
                ApplicationUsage = [new ApplicationUsageInfo { ProcessName = "editor", DurationSeconds = 10 }]
            });

            var file = Assert.Single(fixture.EnumerateFiles("telemetry-*.jsonl"));
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(file.FullName));
            var snapshot = json.RootElement;
            Assert.Equal(7, snapshot.GetProperty("keyboardEvents").GetInt32());
            Assert.Equal(3, snapshot.GetProperty("mouseEvents").GetInt32());
            Assert.Equal("editor", snapshot.GetProperty("dominantProcessName").GetString());
            Assert.False(snapshot.TryGetProperty("dominantWindowTitle", out _));
            var usage = Assert.Single(snapshot.GetProperty("applicationUsage").EnumerateArray());
            Assert.Equal("editor", usage.GetProperty("processName").GetString());
            Assert.False(usage.TryGetProperty("windowTitle", out _));
        }
        finally
        {
            foreach (var file in fixture.EnumerateFiles())
                file.Delete();
            fixture.Delete();
        }
    }
}
