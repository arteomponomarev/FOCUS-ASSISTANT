
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public sealed class FocusStateRecord
{
    private FocusStateRecord()
    {
    }

    public FocusStateRecord(
        Guid focusSessionId,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        FocusStateType state,
        string ruleVersion,
        string? reason = null)
    {
        if (focusSessionId == Guid.Empty)
            throw new ArgumentException(
                "Session ID cannot be empty.",
                nameof(focusSessionId));

        if (endedAt <= startedAt)
            throw new ArgumentException(
                "End time must be after start time.");

        if (!Enum.IsDefined(state))
            throw new ArgumentOutOfRangeException(
                nameof(state));

        if (string.IsNullOrWhiteSpace(ruleVersion))
            throw new ArgumentException(
                "Rule version is required.",
                nameof(ruleVersion));

        if (ruleVersion.Length > 50)
            throw new ArgumentOutOfRangeException(
                nameof(ruleVersion));

        if (reason?.Length > 500)
            throw new ArgumentOutOfRangeException(
                nameof(reason));

        Id = Guid.NewGuid();
        FocusSessionId = focusSessionId;
        StartedAt = startedAt.ToUniversalTime();
        EndedAt = endedAt.ToUniversalTime();
        State = state;
        RuleVersion = ruleVersion.Trim();
        Reason = reason?.Trim();
    }

    public Guid Id { get; private set; }

    public Guid FocusSessionId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset EndedAt { get; private set; }

    public FocusStateType State { get; private set; }

    public string RuleVersion { get; private set; } = null!;

    public string? Reason { get; private set; }

    public FocusSession FocusSession { get; private set; }
        = null!;
}
