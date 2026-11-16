
using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public sealed class FocusSession
{
    private FocusSession()
    {
    }

    public FocusSession(
        Guid userId,
        TimeSpan plannedDuration)
    {
        if (plannedDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plannedDuration),
                "Planned duration must be greater than zero.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        PlannedDuration = plannedDuration;
        Status = SessionStatus.Planned;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public TimeSpan PlannedDuration { get; private set; }

    public SessionStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public int? SelfReportedConcentration { get; private set; }

    public int? SelfReportedProductivity { get; private set; }

    public string? Reflection { get; private set; }

    public User User { get; private set; } = null!;

    public ICollection<SessionGoal> Goals { get; private set; }
        = new List<SessionGoal>();

    public void Start()
    {
        if (Status != SessionStatus.Planned)
        {
            throw new InvalidOperationException(
                "Only a planned session can be started.");
        }

        StartedAt = DateTimeOffset.UtcNow;
        Status = SessionStatus.Active;
    }

    public void Complete(
        int selfReportedConcentration,
        string? reflection = null,
        int? selfReportedProductivity = null)
    {
        if (Status != SessionStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active session can be completed.");
        }

        if (selfReportedConcentration is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selfReportedConcentration),
                "Concentration rating must be between 1 and 5.");
        }

        if (selfReportedProductivity.HasValue &&
            (selfReportedProductivity.Value < 1 ||
             selfReportedProductivity.Value > 5))
        {
            throw new ArgumentOutOfRangeException(
                nameof(selfReportedProductivity),
                "Productivity rating must be between 1 and 5.");
        }

        EndedAt = DateTimeOffset.UtcNow;

        SelfReportedConcentration = selfReportedConcentration;
        SelfReportedProductivity = selfReportedProductivity;

        Reflection = string.IsNullOrWhiteSpace(reflection)
            ? null
            : reflection.Trim();

        Status = SessionStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is SessionStatus.Completed
            or SessionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Completed or cancelled session cannot be cancelled.");
        }

        EndedAt = DateTimeOffset.UtcNow;
        Status = SessionStatus.Cancelled;
    }

    public SessionGoal AddGoal(string description)
    {
        if (Status != SessionStatus.Planned)
        {
            throw new InvalidOperationException(
                "Goals can only be added before the session starts.");
        }

        var goal = new SessionGoal(
            Id,
            description);

        Goals.Add(goal);

        return goal;
    }
}
