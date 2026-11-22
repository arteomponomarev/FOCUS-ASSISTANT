using Focus.Domain.Enums;

namespace Focus.Domain.Entities;

public sealed class FocusSession
{
    public const int MaximumGoals = 100;
    private readonly List<SessionGoal> _goals = [];
    private readonly List<SessionPause> _pauses = [];

    private FocusSession()
    {
    }

    public FocusSession(Guid userId, TimeSpan plannedDuration, DateTimeOffset? now = null, Guid? workProfileId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (plannedDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(plannedDuration), "Planned duration must be positive.");

        Id = Guid.NewGuid();
        UserId = userId;
        WorkProfileId = workProfileId;
        PlannedDuration = plannedDuration;
        Status = SessionStatus.Planned;
        CreatedAt = Now(now);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? WorkProfileId { get; private set; }
    public TimeSpan PlannedDuration { get; private set; }
    public SessionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public int? SelfReportedConcentration { get; private set; }
    public int? SelfReportedProductivity { get; private set; }
    public string? Reflection { get; private set; }
    public User User { get; private set; } = null!;
    public IReadOnlyList<SessionGoal> Goals => _goals.AsReadOnly();
    public IReadOnlyList<SessionPause> Pauses => _pauses.AsReadOnly();

    public void Start(DateTimeOffset? now = null)
    {
        RequireStatus(SessionStatus.Planned, "Only a planned session can be started.");
        StartedAt = Boundary(now);
        Status = SessionStatus.Active;
    }

    public void Pause(DateTimeOffset? now = null)
    {
        RequireStatus(SessionStatus.Active, "Only an active session can be paused.");
        _pauses.Add(new SessionPause(Id, Boundary(now)));
        Status = SessionStatus.Paused;
    }

    public void Resume(DateTimeOffset? now = null)
    {
        RequireStatus(SessionStatus.Paused, "Only a paused session can be resumed.");
        EndPause(Boundary(now));
        Status = SessionStatus.Active;
    }

    public void Complete(int selfReportedConcentration, string? reflection = null,
        int? selfReportedProductivity = null, DateTimeOffset? now = null)
    {
        if (Status is not (SessionStatus.Active or SessionStatus.Paused))
            throw new SessionConflictException("Only a started session can be completed.");
        if (selfReportedConcentration is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(selfReportedConcentration));
        if (selfReportedProductivity is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(selfReportedProductivity));
        if (reflection?.Length > 1000)
            throw new ArgumentOutOfRangeException(nameof(reflection));

        EndedAt = Boundary(now);
        EndPause(EndedAt.Value);
        SelfReportedConcentration = selfReportedConcentration;
        SelfReportedProductivity = selfReportedProductivity;
        Reflection = string.IsNullOrWhiteSpace(reflection) ? null : reflection.Trim();
        Status = SessionStatus.Completed;
    }

    public void Cancel(DateTimeOffset? now = null)
    {
        RequireEditable();
        EndedAt = Boundary(now);
        EndPause(EndedAt.Value);
        Status = SessionStatus.Cancelled;
    }

    public SessionGoal AddGoal(string description)
    {
        RequireStatus(SessionStatus.Planned, "Goals can only be added before the session starts.");
        if (_goals.Count >= MaximumGoals)
            throw new SessionConflictException("The session has reached its goal limit.");

        var goal = new SessionGoal(Id, description);
        _goals.Add(goal);
        return goal;
    }

    public void RenameGoal(Guid goalId, string description)
    {
        RequireStatus(SessionStatus.Planned, "Goal descriptions can only be changed before the session starts.");
        FindGoal(goalId).Rename(description);
    }

    public void RemoveGoal(Guid goalId)
    {
        RequireStatus(SessionStatus.Planned, "Goals can only be removed before the session starts.");
        _goals.Remove(FindGoal(goalId));
    }

    public void SetGoalOutcome(Guid goalId, GoalStatus outcome)
    {
        if (Status is not (SessionStatus.Active or SessionStatus.Paused))
            throw new SessionConflictException("Goal outcomes can only be set during a started session.");

        FindGoal(goalId).SetOutcome(outcome);
    }

    public SessionDurations GetDurations(DateTimeOffset now) => StartedAt is { } start
        ? GetDurations(start, EndedAt ?? now, now)
        : new SessionDurations(TimeSpan.Zero, TimeSpan.Zero);

    public SessionDurations GetDurations(DateTimeOffset from, DateTimeOffset to, DateTimeOffset now)
    {
        if (StartedAt is not { } start)
            return new SessionDurations(TimeSpan.Zero, TimeSpan.Zero);

        var elapsed = Intersection(start, EndedAt ?? now, from, to);
        var paused = TimeSpan.FromTicks(_pauses.Sum(pause => Intersection(
            pause.StartedAt, pause.EndedAt ?? EndedAt ?? now, Max(from, start), Min(to, EndedAt ?? now)).Ticks));
        return new SessionDurations(elapsed, paused > elapsed ? elapsed : paused);
    }

    private DateTimeOffset Boundary(DateTimeOffset? now)
    {
        var boundary = Now(now);
        var last = _pauses.OrderByDescending(x => x.StartedAt).FirstOrDefault();
        var minimum = last?.EndedAt ?? last?.StartedAt ?? StartedAt ?? CreatedAt;
        return Max(boundary, minimum);
    }

    private void EndPause(DateTimeOffset end) => _pauses.SingleOrDefault(x => x.EndedAt is null)?.End(end);

    private SessionGoal FindGoal(Guid id) =>
        _goals.SingleOrDefault(x => x.Id == id) ?? throw new SessionConflictException("Goal does not belong to the session.");

    private void RequireEditable()
    {
        if (Status is SessionStatus.Completed or SessionStatus.Cancelled)
            throw new SessionConflictException("A completed or cancelled session cannot be changed.");
    }

    private void RequireStatus(SessionStatus expected, string message)
    {
        if (Status != expected)
            throw new SessionConflictException(message);
    }

    private static DateTimeOffset Now(DateTimeOffset? value) => (value ?? DateTimeOffset.UtcNow).ToUniversalTime();
    private static DateTimeOffset Max(DateTimeOffset x, DateTimeOffset y) => x > y ? x : y;
    private static DateTimeOffset Min(DateTimeOffset x, DateTimeOffset y) => x < y ? x : y;

    private static TimeSpan Intersection(DateTimeOffset start, DateTimeOffset end, DateTimeOffset from, DateTimeOffset to)
    {
        var length = Min(end, to) - Max(start, from);
        return length > TimeSpan.Zero ? length : TimeSpan.Zero;
    }
}

public sealed record SessionDurations(TimeSpan Elapsed, TimeSpan Paused)
{
    public TimeSpan Active => Elapsed - Paused;
}
