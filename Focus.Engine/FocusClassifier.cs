using Focus.Domain.Enums;

namespace Focus.Engine;

public sealed record SignalWindow(double DurationSeconds, int KeyboardEvents, int MouseClicks,
    int WindowSwitches, int IdleSeconds, bool IsBreak, ProcessCategory ProcessCategory);

public sealed record FocusEstimate(FocusStateType State, string RuleVersion, string Reason);

public sealed class FocusClassifier
{
    public const string Version = "rules-1.0.0";

    public FocusEstimate Evaluate(SignalWindow input)
    {
        if (!double.IsFinite(input.DurationSeconds) || input.DurationSeconds <= 0 ||
            Math.Min(Math.Min(input.KeyboardEvents, input.MouseClicks), Math.Min(input.WindowSwitches, input.IdleSeconds)) < 0 ||
            input.IdleSeconds > input.DurationSeconds || !Enum.IsDefined(input.ProcessCategory))
            throw new ArgumentOutOfRangeException(nameof(input));

        if (input.IsBreak)
            return Make(FocusStateType.Break, "Window falls entirely within a recorded session pause.");

        var idleRatio = input.IdleSeconds / input.DurationSeconds;
        var noInput = input.KeyboardEvents == 0 && input.MouseClicks == 0;
        var switchesPerMinute = 60 * input.WindowSwitches / input.DurationSeconds;
        if (input.DurationSeconds >= 120 && idleRatio >= 0.98 && noInput && input.WindowSwitches == 0)
            return Make(FocusStateType.Absent, "Extended idle window without interaction. Physical presence is unknown.");
        if (idleRatio >= 0.75)
            return Make(FocusStateType.Idle, "Idle time accounts for at least 75% of the window.");
        if (input.ProcessCategory == ProcessCategory.Distracting && switchesPerMinute >= 6 && idleRatio < 0.5)
            return Make(FocusStateType.Distracted, "Profile flags the process and observed switching is elevated.");
        if (input.ProcessCategory == ProcessCategory.Allowed && switchesPerMinute <= 2 && idleRatio <= 0.25 && !noInput)
            return Make(FocusStateType.Focused, "Profile allows the process and the window shows input with low idle time and switching.");

        return Make(FocusStateType.Neutral, "Available signals are insufficient for a stronger estimate.");
    }

    private static FocusEstimate Make(FocusStateType state, string reason) => new(state, Version, reason);
}
