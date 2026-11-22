using System.Net;
using System.Text.Json;
using Focus.Agent.Sync;
using Focus.Domain.Enums;

namespace Focus.Backend.Tests;

public sealed class AgentSyncTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Session = Guid.NewGuid();
    private static readonly Permission Consent = new(true, Start.AddMinutes(-1));

    [Fact]
    public void FinishesARealPartialIntervalAndExcludesSamplesAfterCompletion()
    {
        var output = new List<PendingActivity>();
        var buffer = new CaptureBuffer(Session, output.Add);
        for (var i = 0; i < 17; i++)
            buffer.Add(Sample(i, i + 1));
        buffer.Confirm(At(15, SessionStatus.Active));
        Assert.Equal(15, Assert.Single(output).Item.KeyboardEventCount);

        for (var i = 17; i < 22; i++)
            buffer.Add(Sample(i, i + 1));
        buffer.Confirm(At(22, SessionStatus.Completed, 20));

        Assert.Equal(2, output.Count);
        var tail = output[1].Item;
        Assert.Equal(Start.AddSeconds(15), tail.StartedAt);
        Assert.Equal(Start.AddSeconds(20), tail.EndedAt);
        Assert.Equal(5, tail.KeyboardEventCount);
        Assert.NotEqual(output[0].Item.ClientEventId, tail.ClientEventId);
    }

    [Fact]
    public void PausesDiscardCrossingSamplesAndResumeWithANewInterval()
    {
        var output = new List<PendingActivity>();
        var buffer = new CaptureBuffer(Session, output.Add);
        buffer.Add(Sample(0, 2));
        buffer.Add(Sample(2, 4));
        buffer.Confirm(At(4, SessionStatus.Paused, pauses: [new(Start.AddSeconds(3), null)]));
        Assert.Equal(Start.AddSeconds(2), Assert.Single(output).Item.EndedAt);

        buffer.Add(Sample(5, 7));
        buffer.Confirm(At(7, SessionStatus.Active, pauses: [new(Start.AddSeconds(3), Start.AddSeconds(5))]));
        buffer.Suspend();
        Assert.Equal(2, output.Count);
        Assert.Equal(Start.AddSeconds(5), output[1].Item.StartedAt);
    }

    [Fact]
    public async Task KeepsUnacknowledgedRecordsAcrossRestartAndRetriesTheSameIds()
    {
        var dir = Directory.CreateTempSubdirectory("focus-agent-queue-");
        try
        {
            var entry = Entry();
            var endpoint = new EndpointStub { FailUpload = true };
            using (var queue = TestQueue(dir.FullName))
            {
                queue.Add(entry);
                await Assert.ThrowsAsync<HttpRequestException>(() =>
                    new OfflineSync(endpoint, queue, new Clock()).DeliverAsync(Consent.ConsentedAt!.Value, default));
                Assert.False(queue.IsEmpty);
            }
            var saved = Assert.Single(dir.GetFiles("*.bin")).FullName;
            File.Move(saved, saved + ".tmp");

            endpoint.FailUpload = false;
            using var reopened = TestQueue(dir.FullName);
            var result = await new OfflineSync(endpoint, reopened, new Clock()).DeliverAsync(Consent.ConsentedAt!.Value, default);
            Assert.Equal(1, result.Accepted);
            Assert.True(reopened.IsEmpty);
            Assert.Equal(new[] { entry.Item.ClientEventId, entry.Item.ClientEventId }, endpoint.Sent);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(entry.Item));
            Assert.Equal(new[]
            {
                "ActiveProcessName", "ClientEventId", "EndedAt", "IdleSeconds",
                "KeyboardEventCount", "MouseClickCount", "StartedAt", "WindowSwitchCount"
            }, document.RootElement.EnumerateObject().Select(x => x.Name).Order(StringComparer.Ordinal));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task DeniedConsentStopsAStartedCaptureAndDiscardsItsQueue()
    {
        var directory = Directory.CreateTempSubdirectory("focus-agent-revoke-");
        try
        {
            using var queue = TestQueue(directory.FullName);
            var endpoint = new EndpointStub();
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var clock = new Clock();
            var source = new SignalStub(clock, () =>
            {
                queue.Add(Entry());
                endpoint.Privacy = new(false, null);
            });
            await new SessionAgent(Session, endpoint, source, queue, clock, _ => { }).RunAsync(stop.Token);
            Assert.False(stop.IsCancellationRequested);
            Assert.True(source.Started);
            Assert.True(source.Stopped);
            Assert.True(queue.IsEmpty);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ShutdownStopsHooksAndFlushesConfirmedPartialData()
    {
        var directory = Directory.CreateTempSubdirectory("focus-agent-stop-");
        try
        {
            using var queue = TestQueue(directory.FullName);
            var endpoint = new EndpointStub();
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var clock = new Clock();
            var source = new SignalStub(clock, stop.Cancel);
            await new SessionAgent(Session, endpoint, source, queue, clock, _ => { }).RunAsync(stop.Token);
            Assert.True(source.Started);
            Assert.True(source.Stopped);
            Assert.Single(endpoint.Sent);
            Assert.True(queue.IsEmpty);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void RetryHonorsServerDelayAndApiOriginRejectsInsecureRemoteCredentials()
    {
        var retry = new RetrySchedule();
        Assert.Equal(TimeSpan.FromSeconds(1), retry.Fail(new HttpRequestException()));
        Assert.Equal(TimeSpan.FromSeconds(2), retry.Fail(new HttpRequestException()));
        Assert.Equal(TimeSpan.FromSeconds(21), retry.Fail(new RemoteFailure(HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(21))));
        retry.Reset();
        Assert.Equal(TimeSpan.FromSeconds(1), retry.Fail(new HttpRequestException()));
        Assert.Throws<ArgumentException>(() => AgentApi.ParseOrigin("http://example.invalid"));
        Assert.Throws<ArgumentException>(() => AgentApi.ParseOrigin("https://user@example.invalid"));
        Assert.Equal("http://127.0.0.1:8080/", AgentApi.ParseOrigin("http://127.0.0.1:8080").AbsoluteUri);
    }

    [AgentWindowsFact]
    public void QueueProtectsAggregatesWithWindowsProfileEncryption()
    {
        var directory = Directory.CreateTempSubdirectory("focus-agent-dpapi-");
        try
        {
            var origin = AgentApi.ParseOrigin("http://localhost:8080");
            var account = Guid.NewGuid();
            var entry = Entry();
            using (var queue = ActivityQueue.ForWindowsAccount(origin, account, directory.FullName))
                queue.Add(entry);
            var encrypted = Assert.Single(directory.GetFiles("*.bin", SearchOption.AllDirectories));
            Assert.DoesNotContain("editor", System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(encrypted.FullName)));
            using var reopened = ActivityQueue.ForWindowsAccount(origin, account, directory.FullName);
            Assert.Equal(entry, Assert.Single(reopened.Pending(Start.AddSeconds(20), Consent.ConsentedAt!.Value)));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static SignalSample Sample(int from, int to) =>
        new(Start.AddSeconds(from), Start.AddSeconds(to), "editor", 1, 0, 0, 0);

    private static Authority At(int now, SessionStatus status, int? end = null, PauseWindow[]? pauses = null) =>
        new(Consent, new SessionWindow(status, Start, end is null ? null : Start.AddSeconds(end.Value),
            pauses ?? []), Start.AddSeconds(now));

    private static ActivityQueue TestQueue(string path) => new(path, x => x, x => x);

    private static PendingActivity Entry() =>
        new(Session, new Activity(Guid.NewGuid(), Start, Start.AddSeconds(1), "editor", 1, 0, 0, 0));

    private sealed class Clock : TimeProvider
    {
        private readonly long _since = TimeProvider.System.GetTimestamp();
        private long _offset = TimeSpan.FromSeconds(20).Ticks;
        public override DateTimeOffset GetUtcNow() =>
            Start.AddTicks(Interlocked.Read(ref _offset)) + TimeProvider.System.GetElapsedTime(_since);
        public void Advance() => Interlocked.Add(ref _offset, TimeSpan.FromSeconds(1).Ticks);
    }

    private sealed class EndpointStub : IAgentEndpoint
    {
        public Permission Privacy { get; set; } = Consent;
        public bool FailUpload { get; set; }
        public List<Guid> Sent { get; } = [];

        public Task<Authority> CheckAsync(Guid session, CancellationToken ct) =>
            Task.FromResult(At(30, SessionStatus.Active) with { Privacy = Privacy });

        public Task<UploadReceipt> UploadAsync(Guid session, IReadOnlyList<Activity> items, CancellationToken ct)
        {
            if (!Privacy.TelemetryEnabled)
                return Task.FromException<UploadReceipt>(new RemoteFailure(HttpStatusCode.Forbidden, null));
            Sent.AddRange(items.Select(x => x.ClientEventId));
            return FailUpload
                ? Task.FromException<UploadReceipt>(new HttpRequestException("Synthetic network failure."))
                : Task.FromResult(new UploadReceipt(0, items.Count));
        }
    }

    private sealed class SignalStub(Clock clock, Action firstSample) : ISignalSource
    {
        private bool _fired;
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public void Start() => Started = true;

        public SignalSample? Sample()
        {
            if (_fired)
                return null;
            _fired = true;
            var start = clock.GetUtcNow();
            clock.Advance();
            firstSample();
            return new SignalSample(start, clock.GetUtcNow(), "editor", 1, 1, 0, 0);
        }

        public void Stop() => Stopped = true;
    }
}

public sealed class AgentWindowsFactAttribute : FactAttribute
{
    public AgentWindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows DPAPI verification requires a Windows user profile.";
    }
}
