using System.ComponentModel.DataAnnotations;
using Focus.Api.Hosting;
using Focus.Application.Common;
using Focus.Application.Sessions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Focus.Api.Controllers;

[ApiController]
[Route("api/v1/sessions")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class SessionsController(SessionService sessions, TimeProvider clock, ILogger<SessionsController> logger,
    IOptions<HttpApiOptions> options) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SessionResponse>(201)]
    public async Task<ActionResult<SessionResponse>> Create(CreateSessionRequest request, CancellationToken ct)
    {
        var session = await sessions.CreateAsync(User.UserId(), request.PlannedDurationSeconds, request.Goals, ct, request.WorkProfileId);
        logger.LogInformation("Session {SessionId} created", session.Id);
        return CreatedAtAction(nameof(Get), new { id = session.Id }, Project(session));
    }

    [HttpGet]
    [ProducesResponseType<Page<SessionResponse>>(200)]
    public async Task<Page<SessionResponse>> List([FromQuery] SessionListQuery query, CancellationToken ct)
    {
        if (query.Status is { } status && !Enum.IsDefined(status))
            throw new UseCaseException(Failure.Invalid, "Invalid session status.");

        var filter = new SessionFilter(query.GetPage(options.Value.MaximumPageSize), query.From, query.To, query.Status, query.Sort == "desc");
        return (await sessions.ListAsync(User.UserId(), filter, ct)).Select(Project);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<SessionResponse>(200)]
    public async Task<SessionResponse> Get(Guid id, CancellationToken ct) =>
        Project(await sessions.GetAsync(User.UserId(), id, ct));

    [HttpPost("{id}/start")]
    [ProducesResponseType<SessionResponse>(200)]
    [ProducesResponseType<ProblemDetails>(409)]
    public Task<SessionResponse> Start(Guid id, CancellationToken ct) => Transition(id, SessionCommand.Start, ct);

    [HttpPost("{id}/pause")]
    [ProducesResponseType<SessionResponse>(200)]
    [ProducesResponseType<ProblemDetails>(409)]
    public Task<SessionResponse> Pause(Guid id, CancellationToken ct) => Transition(id, SessionCommand.Pause, ct);

    [HttpPost("{id}/resume")]
    [ProducesResponseType<SessionResponse>(200)]
    [ProducesResponseType<ProblemDetails>(409)]
    public Task<SessionResponse> Resume(Guid id, CancellationToken ct) => Transition(id, SessionCommand.Resume, ct);

    [HttpPost("{id}/cancel")]
    [ProducesResponseType<SessionResponse>(200)]
    [ProducesResponseType<ProblemDetails>(409)]
    public Task<SessionResponse> Cancel(Guid id, CancellationToken ct) => Transition(id, SessionCommand.Cancel, ct);

    [HttpPost("{id}/complete")]
    [ProducesResponseType<SessionResponse>(200)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<SessionResponse> Complete(Guid id, CompleteSessionRequest request, CancellationToken ct)
    {
        var session = await sessions.CompleteAsync(User.UserId(), id, request.SelfReportedConcentration,
            request.SelfReportedProductivity, request.Reflection, request.GoalOutcomes, ct);
        logger.LogInformation("Session {SessionId} transitioned to {SessionStatus}", session.Id, session.Status);
        return Project(session);
    }

    [HttpGet("{id}/goals")]
    [ProducesResponseType<GoalResponse[]>(200)]
    public async Task<GoalResponse[]> ListGoals(Guid id, CancellationToken ct) =>
        (await sessions.GetAsync(User.UserId(), id, ct)).Goals.OrderBy(x => x.Id).Select(GoalResponse.From).ToArray();

    [HttpGet("{id}/goals/{goalId}")]
    [ProducesResponseType<GoalResponse>(200)]
    public async Task<GoalResponse> GetGoal(Guid id, Guid goalId, CancellationToken ct)
    {
        var session = await sessions.GetAsync(User.UserId(), id, ct);
        return GoalResponse.From(session.Goals.FirstOrDefault(x => x.Id == goalId) ?? throw UseCaseException.NotFound());
    }

    [HttpPost("{id}/goals")]
    [ProducesResponseType<GoalResponse>(201)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<GoalResponse>> AddGoal(Guid id, CreateGoalRequest request, CancellationToken ct)
    {
        var goal = await sessions.AddGoalAsync(User.UserId(), id, request.Description, ct);
        return CreatedAtAction(nameof(GetGoal), new { id, goalId = goal.Id }, GoalResponse.From(goal));
    }

    [HttpPatch("{id}/goals/{goalId}")]
    [ProducesResponseType<GoalResponse>(200)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<GoalResponse> UpdateGoal(Guid id, Guid goalId, UpdateGoalRequest request, CancellationToken ct) =>
        GoalResponse.From(await sessions.UpdateGoalAsync(User.UserId(), id, goalId, request.Description, request.Outcome, ct));

    [HttpDelete("{id}/goals/{goalId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> DeleteGoal(Guid id, Guid goalId, CancellationToken ct)
    {
        await sessions.DeleteGoalAsync(User.UserId(), id, goalId, ct);
        return NoContent();
    }

    private async Task<SessionResponse> Transition(Guid id, SessionCommand command, CancellationToken ct)
    {
        var session = await sessions.TransitionAsync(User.UserId(), id, command, ct);
        logger.LogInformation("Session {SessionId} transitioned to {SessionStatus}", session.Id, session.Status);
        return Project(session);
    }

    private SessionResponse Project(FocusSession session)
    {
        var duration = session.GetDurations(clock.GetUtcNow());
        var goals = session.Goals.OrderBy(x => x.Id).Select(GoalResponse.From).ToArray();
        return new SessionResponse(session.Id, session.WorkProfileId, session.Status, session.PlannedDuration.TotalSeconds,
            session.CreatedAt, session.StartedAt, session.EndedAt, duration.Elapsed.TotalSeconds,
            duration.Paused.TotalSeconds, duration.Active.TotalSeconds,
            session.SelfReportedConcentration, session.SelfReportedProductivity, session.Reflection,
            goals, GoalStatistics.From(session.Goals));
    }
}

public sealed record CreateSessionRequest(
    [Range(1, 86400)] int PlannedDurationSeconds,
    [MaxLength(FocusSession.MaximumGoals)] IReadOnlyList<string>? Goals = null,
    Guid? WorkProfileId = null);

public sealed record CompleteSessionRequest(
    [Range(1, 5)] int SelfReportedConcentration,
    [Range(1, 5)] int SelfReportedProductivity,
    [StringLength(1000)] string? Reflection = null,
    [MaxLength(FocusSession.MaximumGoals)] IReadOnlyList<GoalOutcome>? GoalOutcomes = null);

public sealed record CreateGoalRequest([Required, StringLength(500)] string Description);
public sealed record UpdateGoalRequest([StringLength(500)] string? Description, GoalStatus? Outcome);

public class PagingQuery
{
    [Range(1, 100_000)] public int Page { get; init; } = 1;
    [Range(1, 500)] public int PageSize { get; init; } = 25;

    public PageRequest GetPage(int maximumPageSize)
    {
        if (PageSize > maximumPageSize)
            throw new UseCaseException(Failure.Invalid, "Page size exceeds the configured maximum.");
        return new PageRequest(Page, PageSize);
    }
}

public sealed class SessionListQuery : PagingQuery
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public SessionStatus? Status { get; init; }
    [RegularExpression("asc|desc")] public string Sort { get; init; } = "desc";
}

public sealed record GoalResponse(Guid Id, string Description, GoalStatus Status)
{
    public static GoalResponse From(SessionGoal goal) => new(goal.Id, goal.Description, goal.Status);
}

public sealed record SessionResponse(Guid Id, Guid? WorkProfileId, SessionStatus Status, double PlannedDurationSeconds, DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt, DateTimeOffset? EndedAt, double ElapsedSeconds, double PausedSeconds, double ActiveSeconds,
    int? SelfReportedConcentration, int? SelfReportedProductivity, string? Reflection,
    GoalResponse[] Goals, GoalStatistics GoalStatistics);
