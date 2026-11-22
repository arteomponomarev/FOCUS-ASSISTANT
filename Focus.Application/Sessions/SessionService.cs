using Focus.Application.Common;
using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Application.Sessions;

public enum SessionCommand { Start, Pause, Resume, Cancel }

public sealed record GoalOutcome(Guid GoalId, GoalStatus Status);

public sealed class SessionService(ISessionStore store, TimeProvider clock)
{
    public async Task<FocusSession> CreateAsync(Guid user, int plannedSeconds, IReadOnlyList<string>? goals,
        CancellationToken ct, Guid? profileId = null)
    {
        if (plannedSeconds is < 1 or > 86_400 || goals?.Count > FocusSession.MaximumGoals)
            throw new UseCaseException(Failure.Invalid, "Duration or goal count is outside the supported bounds.");

        var session = new FocusSession(user, TimeSpan.FromSeconds(plannedSeconds), clock.GetUtcNow(), profileId);
        foreach (var description in goals ?? [])
            session.AddGoal(ValidateDescription(description));

        await store.AddAsync(session, ct);
        return session;
    }

    public async Task<FocusSession> GetAsync(Guid user, Guid id, CancellationToken ct) =>
        await store.FindAsync(user, id, ct) ?? throw UseCaseException.NotFound();

    public Task<Page<FocusSession>> ListAsync(Guid user, SessionFilter filter, CancellationToken ct)
    {
        if (filter.From is { } from && filter.To is { } to && from >= to)
            throw new UseCaseException(Failure.Invalid, "The from timestamp must be earlier than to.");

        return store.ListAsync(user, filter with { From = filter.From?.ToUniversalTime(), To = filter.To?.ToUniversalTime() }, ct);
    }

    public Task<FocusSession> TransitionAsync(Guid user, Guid id, SessionCommand command, CancellationToken ct) =>
        UpdateAsync(user, id, session =>
        {
            var now = clock.GetUtcNow();
            switch (command)
            {
                case SessionCommand.Start: session.Start(now); break;
                case SessionCommand.Pause: session.Pause(now); break;
                case SessionCommand.Resume: session.Resume(now); break;
                case SessionCommand.Cancel: session.Cancel(now); break;
                default: throw new UseCaseException(Failure.Invalid, "Unknown session command.");
            }

            return session;
        }, ct);

    public Task<FocusSession> CompleteAsync(Guid user, Guid id, int concentration, int productivity,
        string? reflection, IReadOnlyList<GoalOutcome>? outcomes, CancellationToken ct)
    {
        if (concentration is < 1 or > 5 || productivity is < 1 or > 5 || reflection?.Length > 1000 ||
            reflection?.Contains('\0') == true)
            throw new UseCaseException(Failure.Invalid, "Ratings must be between 1 and 5 and reflection must be at most 1000 characters.");

        if (outcomes is not null && (outcomes.Count > FocusSession.MaximumGoals ||
            outcomes.Any(x => x is null || x.GoalId == Guid.Empty || !ValidOutcome(x.Status)) ||
            outcomes.Select(x => x.GoalId).Distinct().Count() != outcomes.Count))
            throw new UseCaseException(Failure.Invalid, "Goal outcomes must use distinct IDs and an outcome status.");

        return UpdateAsync(user, id, session =>
        {
            foreach (var outcome in outcomes ?? [])
            {
                RequireGoal(session, outcome.GoalId);
                session.SetGoalOutcome(outcome.GoalId, outcome.Status);
            }

            session.Complete(concentration, reflection, productivity, clock.GetUtcNow());
            return session;
        }, ct);
    }

    public Task<SessionGoal> AddGoalAsync(Guid user, Guid id, string description, CancellationToken ct) =>
        UpdateAsync(user, id, session => session.AddGoal(ValidateDescription(description)), ct);

    public Task<SessionGoal> UpdateGoalAsync(Guid user, Guid id, Guid goalId, string? description, GoalStatus? outcome, CancellationToken ct)
    {
        if ((description is null && outcome is null) || (outcome is { } status && !ValidOutcome(status)))
            throw new UseCaseException(Failure.Invalid, "Provide a description or a valid goal outcome.");

        return UpdateAsync(user, id, session =>
        {
            var goal = RequireGoal(session, goalId);
            if (description is not null)
                session.RenameGoal(goalId, ValidateDescription(description));
            if (outcome is { } result)
                session.SetGoalOutcome(goalId, result);
            return goal;
        }, ct);
    }

    public Task DeleteGoalAsync(Guid user, Guid id, Guid goalId, CancellationToken ct) =>
        UpdateAsync(user, id, session =>
        {
            RequireGoal(session, goalId);
            session.RemoveGoal(goalId);
            return true;
        }, ct);

    private async Task<T> UpdateAsync<T>(Guid user, Guid id, Func<FocusSession, T> operation, CancellationToken ct)
    {
        try
        {
            return await store.UpdateAsync(user, id, (session, _) => Task.FromResult(operation(session)), ct);
        }
        catch (SessionConflictException exception)
        {
            throw UseCaseException.Conflict(exception.Message);
        }
    }

    private static string ValidateDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 500 || trimmed.Contains('\0'))
            throw new UseCaseException(Failure.Invalid, "A goal description must contain 1 to 500 characters.");

        return trimmed;
    }

    private static bool ValidOutcome(GoalStatus status) =>
        status is GoalStatus.Completed or GoalStatus.PartiallyCompleted or GoalStatus.NotCompleted;

    private static SessionGoal RequireGoal(FocusSession session, Guid goalId) =>
        session.Goals.FirstOrDefault(goal => goal.Id == goalId) ?? throw UseCaseException.NotFound();
}
