
namespace Focus.Domain.Entities;

public sealed class ActivityAggregate
{
    public static readonly TimeSpan MaximumUploadAge = TimeSpan.FromHours(24);

    private ActivityAggregate()
    {
    }

    public ActivityAggregate(
        Guid focusSessionId,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        string? activeProcessName,
        int keyboardEventCount,
        int mouseClickCount,
        int windowSwitchCount,
        int idleSeconds,
        Guid? clientEventId = null)
    {
        if (focusSessionId == Guid.Empty)
            throw new ArgumentException(
                "Session ID cannot be empty.",
                nameof(focusSessionId));

        if (endedAt <= startedAt)
            throw new ArgumentException(
                "End time must be after start time.");

        if (keyboardEventCount < 0 ||
            mouseClickCount < 0 ||
            windowSwitchCount < 0 ||
            idleSeconds < 0)
            throw new ArgumentOutOfRangeException(
                nameof(keyboardEventCount),
                "Activity values cannot be negative.");

        Id = Guid.NewGuid();
        FocusSessionId = focusSessionId;
        ClientEventId = clientEventId;

        StartedAt = startedAt.ToUniversalTime();
        EndedAt = endedAt.ToUniversalTime();

        ActiveProcessName =
            string.IsNullOrWhiteSpace(activeProcessName)
                ? null
                : activeProcessName.Trim();

        KeyboardEventCount = keyboardEventCount;
        MouseClickCount = mouseClickCount;
        WindowSwitchCount = windowSwitchCount;
        IdleSeconds = idleSeconds;
    }

    public Guid Id { get; private set; }

    public Guid FocusSessionId { get; private set; }
    public Guid? ClientEventId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset EndedAt { get; private set; }

    public string? ActiveProcessName { get; private set; }

    public int KeyboardEventCount { get; private set; }

    public int MouseClickCount { get; private set; }

    public int WindowSwitchCount { get; private set; }

    public int IdleSeconds { get; private set; }

    public FocusSession FocusSession { get; private set; }
        = null!;
}
