namespace Focus.Agent.Sync;

internal readonly record struct VisionSlice(long PresentTicks, long NotDetectedTicks,
    long FacingTicks, long AwayTicks, int ValidSamples);

internal sealed class VisionSamples(TimeSpan maximumGap)
{
    private readonly Queue<(DateTimeOffset Start, DateTimeOffset End, char Code)> _spans = new();
    private (DateTimeOffset At, char Code)? _previous;

    public void Add(DateTimeOffset at, char code)
    {
        if (_previous is { } previous && at > previous.At && at - previous.At <= maximumGap)
        {
            var stable = previous.Code == code ? code
                : IsPresent(previous.Code) && IsPresent(code) ? 'P' : 'U';
            if (_spans.Count >= 64)
                _spans.Dequeue();
            _spans.Enqueue((previous.At, at, stable));
        }
        _previous = (at, code);
    }

    public VisionSlice Take(DateTimeOffset start, DateTimeOffset end)
    {
        long present = 0, missing = 0, facing = 0, away = 0;
        var samples = 0;
        while (_spans.TryPeek(out var span))
        {
            if (span.Start >= end)
                break;
            var from = span.Start > start ? span.Start : start;
            var to = span.End < end ? span.End : end;
            if (to > from && span.Code != 'U')
            {
                var ticks = (to - from).Ticks;
                if (span.Code == 'N')
                    missing += ticks;
                else
                    present += ticks;
                if (span.Code == 'F')
                    facing += ticks;
                if (span.Code == 'A')
                    away += ticks;
                if (span.End <= end)
                    samples++;
            }
            if (span.End > end)
                break;
            _spans.Dequeue();
        }
        return new VisionSlice(present, missing, facing, away, samples);
    }

    public void Clear()
    {
        _previous = null;
        _spans.Clear();
    }

    private static bool IsPresent(char code) => code is 'P' or 'F' or 'A';
}
