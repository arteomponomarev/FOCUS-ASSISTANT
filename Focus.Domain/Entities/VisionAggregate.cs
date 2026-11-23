namespace Focus.Domain.Entities;

public sealed record VisionAggregate(int PresentMs, int NotDetectedMs, int UnknownMs,
    int FacingMs, int AwayMs, int ValidSamples)
{
    public bool Fits(long durationMs)
    {
        var observed = (long)PresentMs + NotDetectedMs;
        return PresentMs >= 0 && NotDetectedMs >= 0 && UnknownMs >= 0 && FacingMs >= 0 && AwayMs >= 0 &&
               observed + UnknownMs == durationMs && (long)FacingMs + AwayMs <= PresentMs &&
               ValidSamples is >= 0 and <= 3000 && ValidSamples <= durationMs * 10 / 1000 + 2 &&
               (observed != 0 || ValidSamples == 0);
    }
}
