using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public sealed class SessionGoal
{
    private SessionGoal()
    {
    }

    public SessionGoal(
        Guid focusSessionId,
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Goal description cannot be empty.",
                nameof(description));
        }

        Id = Guid.NewGuid();
        FocusSessionId = focusSessionId;
        Description = description.Trim();
        Status = GoalStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid FocusSessionId { get; private set; }

    public string Description { get; private set; }
        = string.Empty;

    public GoalStatus Status { get; private set; }

    public FocusSession FocusSession { get; private set; } = null!;

    public void MarkCompleted()
    {
        Status = GoalStatus.Completed;
    }

    public void MarkNotCompleted()
    {
        Status = GoalStatus.NotCompleted;
    }
}