using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public sealed class SessionGoal
{
    private SessionGoal()
    {
    }

    public SessionGoal(Guid focusSessionId, string description)
    {
        if (focusSessionId == Guid.Empty)
            throw new ArgumentException("Session ID cannot be empty.", nameof(focusSessionId));

        Id = Guid.NewGuid();
        FocusSessionId = focusSessionId;
        Rename(description);
    }

    public Guid Id { get; private set; }
    public Guid FocusSessionId { get; private set; }
    public string Description { get; private set; } = "";
    public GoalStatus Status { get; private set; } = GoalStatus.Pending;
    public FocusSession FocusSession { get; private set; } = null!;

    internal void Rename(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (description.Trim().Length > 500)
            throw new ArgumentOutOfRangeException(nameof(description));

        Description = description.Trim();
    }

    internal void SetOutcome(GoalStatus outcome)
    {
        if (outcome is not (GoalStatus.Completed or GoalStatus.PartiallyCompleted or GoalStatus.NotCompleted))
            throw new ArgumentOutOfRangeException(nameof(outcome));

        Status = outcome;
    }
}

public sealed record GoalStatistics(int Total, int Completed, int PartiallyCompleted, double CompletionPercentage)
{
    public static GoalStatistics From(IEnumerable<SessionGoal> goals)
    {
        var byStatus = goals.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count());
        var total = byStatus.Values.Sum();
        var completed = byStatus.GetValueOrDefault(GoalStatus.Completed);
        var partial = byStatus.GetValueOrDefault(GoalStatus.PartiallyCompleted);
        return new GoalStatistics(total, completed, partial, total == 0 ? 0 : 100 * (completed + 0.5 * partial) / total);
    }
}
