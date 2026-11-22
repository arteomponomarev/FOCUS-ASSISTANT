namespace Focus.Domain.Entities;

public enum AvatarVisibility { AlwaysVisible, OnlyWhenNeeded, NotificationsOnly, Hidden }
public enum NotificationIntensity { Quiet, Balanced, Active }

public sealed record PomodoroSettings(int FocusMinutes, int ShortBreakMinutes, int LongBreakMinutes, int IntervalsBeforeLongBreak)
{
    public static readonly PomodoroSettings Default = new(25, 5, 15, 4);
}

public sealed record AvatarSettings(AvatarVisibility Visibility, NotificationIntensity Intensity, string? AvatarId)
{
    public static readonly AvatarSettings Default = new(AvatarVisibility.OnlyWhenNeeded, NotificationIntensity.Balanced, null);
}

public sealed class UserPreferences
{
    private UserPreferences()
    {
    }

    public UserPreferences(Guid userId)
    {
        UserId = userId;
        SetPomodoro(PomodoroSettings.Default);
        SetAvatar(AvatarSettings.Default);
    }

    public Guid UserId { get; private set; }
    public int FocusMinutes { get; private set; }
    public int ShortBreakMinutes { get; private set; }
    public int LongBreakMinutes { get; private set; }
    public int IntervalsBeforeLongBreak { get; private set; }
    public AvatarVisibility Visibility { get; private set; }
    public NotificationIntensity Intensity { get; private set; }
    public string? AvatarId { get; private set; }

    public PomodoroSettings GetPomodoro() => new(FocusMinutes, ShortBreakMinutes, LongBreakMinutes, IntervalsBeforeLongBreak);

    public AvatarSettings GetAvatar() => new(Visibility, Intensity, AvatarId);

    public void SetPomodoro(PomodoroSettings value)
    {
        FocusMinutes = value.FocusMinutes;
        ShortBreakMinutes = value.ShortBreakMinutes;
        LongBreakMinutes = value.LongBreakMinutes;
        IntervalsBeforeLongBreak = value.IntervalsBeforeLongBreak;
    }

    public void SetAvatar(AvatarSettings value)
    {
        Visibility = value.Visibility;
        Intensity = value.Intensity;
        AvatarId = value.AvatarId;
    }
}
