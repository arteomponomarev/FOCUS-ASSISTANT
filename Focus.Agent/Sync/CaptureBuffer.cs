using Focus.Domain.Enums;

namespace Focus.Agent.Sync;

internal sealed record SignalSample(DateTimeOffset StartedAt, DateTimeOffset EndedAt,
    string? ProcessName, int Keys, int Clicks, int Switches, double IdleSeconds);

internal interface ISignalSource
{
    void Start();
    SignalSample? Sample();
    void Stop();
}

internal sealed class CaptureBuffer(Guid sessionId, Action<PendingActivity> save)
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);
    private readonly Queue<SignalSample> _pending = new();
    private readonly List<SignalSample> _interval = [];
    private DateTimeOffset? _consentedAt;

    public void Add(SignalSample sample)
    {
        if (_pending.Count >= 256)
            throw new InvalidOperationException("Too many unconfirmed samples. Check clock synchronization.");
        if (sample.EndedAt > sample.StartedAt)
            _pending.Enqueue(sample);
    }

    public void Confirm(Authority authority)
    {
        if (!authority.Privacy.TelemetryEnabled || authority.Privacy.ConsentedAt is not { } consent)
        {
            Discard();
            return;
        }

        if (_consentedAt is { } previous && previous != consent)
            Discard();
        _consentedAt = consent;

        if (authority.Session is not { StartedAt: { } start } session ||
            session.Status is SessionStatus.Planned or SessionStatus.Cancelled)
        {
            Discard();
            return;
        }

        var terminal = session.Status == SessionStatus.Completed;
        var bound = session.EndedAt ?? authority.CheckedThrough;
        while (_pending.TryPeek(out var sample))
        {
            if (sample.EndedAt > bound)
                break;
            _pending.Dequeue();
            if (sample.StartedAt < start || sample.StartedAt < consent || session.Pauses.Any(pause =>
                    sample.EndedAt > pause.StartedAt && (pause.EndedAt is null || sample.StartedAt < pause.EndedAt)))
            {
                Flush();
                continue;
            }

            if (_interval.Count > 0 && _interval[^1].EndedAt != sample.StartedAt)
                Flush();

            _interval.Add(sample);
            if (sample.EndedAt - _interval[0].StartedAt >= Interval)
                Flush();
        }

        if (terminal || session.Status != SessionStatus.Active)
        {
            _pending.Clear();
            Flush();
        }
    }

    public void Suspend()
    {
        _pending.Clear();
        Flush();
    }

    public void FlushConfirmed() => Flush();

    public void Discard()
    {
        _pending.Clear();
        _interval.Clear();
        _consentedAt = null;
    }

    private void Flush()
    {
        if (_interval.Count == 0)
            return;

        var start = _interval[0].StartedAt;
        var end = _interval[^1].EndedAt;
        var process = _interval.Where(sample => sample.ProcessName is not null)
            .GroupBy(sample => sample.ProcessName)
            .MaxBy(group => group.Sum(sample => (sample.EndedAt - sample.StartedAt).Ticks))?.Key;
        save(new PendingActivity(sessionId, new Activity(Guid.NewGuid(), start, end, process,
            Count(sample => sample.Keys), Count(sample => sample.Clicks), Count(sample => sample.Switches),
            (int)Math.Clamp(_interval.Sum(sample => sample.IdleSeconds), 0, (end - start).TotalSeconds))));
        _interval.Clear();
    }

    private int Count(Func<SignalSample, int> select) => (int)Math.Min(100_000, _interval.Sum(sample => (long)select(sample)));
}
