using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Focus.Api.Hosting;
using Focus.Application.Common;
using Focus.Application.Research;
using Focus.Application.Sessions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Focus.Api.Controllers;

[ApiController]
[Route("api/v1/sessions/{id}")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class ResearchController(
    SessionService sessions, TelemetryService telemetry, IResearchStore queries, VisionResearch vision, IOptions<HttpApiOptions> options,
    ILogger<ResearchController> logger, ApiRuntimeMetrics metrics) : ControllerBase
{
    [HttpPost("activity/batch")]
    [ProducesResponseType<IngestionResult>(200)]
    [RequestSizeLimit(128_000)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(413)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<IngestionResult> Upload(Guid id, ActivityBatch request, CancellationToken ct)
    {
        var items = request.Items.Select(item => item?.ToInput()!).ToArray();
        var result = await telemetry.AcceptAsync(User.UserId(), id, items, ct);
        metrics.ActivityPersisted(result.Created);
        logger.LogInformation("Telemetry batch persisted for session {SessionId}: {CreatedCount} accepted, {DuplicateCount} duplicate",
            id, result.Created, result.Duplicates);
        return result;
    }

    [HttpGet("activity")]
    [ProducesResponseType<Page<ActivityResponse>>(200)]
    public async Task<Page<ActivityResponse>> Activity(Guid id, [FromQuery] PagingQuery query, CancellationToken ct)
    {
        var owner = User.UserId();
        await sessions.GetAsync(owner, id, ct);
        var page = await queries.ListActivityAsync(owner, id, query.GetPage(options.Value.MaximumPageSize), ct);
        return page.Select(ActivityResponse.From);
    }

    [HttpGet("focus-states")]
    [ProducesResponseType<Page<EstimateResponse>>(200)]
    public async Task<Page<EstimateResponse>> Estimates(Guid id, [FromQuery] PagingQuery query, CancellationToken ct)
    {
        var owner = User.UserId();
        await sessions.GetAsync(owner, id, ct);
        var page = await queries.ListEstimatesAsync(owner, id, query.GetPage(options.Value.MaximumPageSize), ct);
        return page.Select(EstimateResponse.From);
    }

    [HttpGet("focus-states/current")]
    [ProducesResponseType<CurrentStateResponse>(200)]
    public async Task<CurrentStateResponse> Current(Guid id, CancellationToken ct)
    {
        var owner = User.UserId();
        var session = await sessions.GetAsync(owner, id, ct);
        var state = await queries.GetLatestEstimateAsync(owner, id, ct);
        var response = state is null ? null : EstimateResponse.From(state);
        if (state is not null && response is not null && session.Status == SessionStatus.Active &&
            await queries.UsesExperimentalVisionAsync(owner, ct) &&
            await queries.GetCameraConsentAsync(owner, ct) is { } consent &&
            await vision.CompareAsync(owner, state, ct, consent) is { Experimental: var estimated })
            response = response with { EstimatedState = estimated.State, RuleVersion = estimated.RuleVersion, Reason = estimated.Reason };

        return new CurrentStateResponse(session.Status, response,
            session.Goals.Select(GoalResponse.From).ToArray());
    }

    [HttpGet("vision")]
    [ProducesResponseType<VisionReport>(200)]
    public async Task<VisionReport> Vision(Guid id, CancellationToken ct)
    {
        var owner = User.UserId();
        await sessions.GetAsync(owner, id, ct);
        return await vision.ReadAsync(owner, id, ct);
    }

    [HttpGet("timeline")]
    [ProducesResponseType<TimelineResponse>(200)]
    public async Task<TimelineResponse> Timeline(Guid id, [FromQuery] PagingQuery query, CancellationToken ct)
    {
        var owner = User.UserId();
        var session = await sessions.GetAsync(owner, id, ct);
        var events = await queries.ListTimelineAsync(owner, id, query.GetPage(options.Value.MaximumPageSize), ct);
        return new TimelineResponse(session.Status, session.CreatedAt, session.StartedAt, session.EndedAt,
            session.Pauses.OrderBy(x => x.StartedAt).Select(x => new PauseResponse(x.StartedAt, x.EndedAt)).ToArray(),
            session.Goals.Select(GoalResponse.From).ToArray(), GoalStatistics.From(session.Goals), events);
    }
}

public sealed record ActivityBatch([Required, MinLength(1), MaxLength(TelemetryService.BatchLimit)] IReadOnlyList<ActivityInput> Items);

public sealed record ActivityInput(
    [property: JsonRequired] Guid ClientEventId,
    [property: JsonRequired] DateTimeOffset StartedAt,
    [property: JsonRequired] DateTimeOffset EndedAt,
    [property: JsonRequired][Range(0, TelemetryService.CounterLimit)] int KeyboardEventCount,
    [property: JsonRequired][Range(0, TelemetryService.CounterLimit)] int MouseClickCount,
    [property: JsonRequired][Range(0, TelemetryService.CounterLimit)] int WindowSwitchCount,
    [property: JsonRequired][Range(0, 300)] int IdleSeconds,
    [StringLength(200)] string? ActiveProcessName = null,
    VisionInput? Vision = null)
{
    public AggregateInput ToInput() => new(ClientEventId, StartedAt, EndedAt, ActiveProcessName, KeyboardEventCount,
        MouseClickCount, WindowSwitchCount, IdleSeconds, Vision?.ToAggregate());
}

public sealed record VisionInput(
    [property: JsonRequired] int PresentMs, [property: JsonRequired] int NotDetectedMs,
    [property: JsonRequired] int UnknownMs, [property: JsonRequired] int FacingMs,
    [property: JsonRequired] int AwayMs, [property: JsonRequired] int ValidSamples)
{
    public VisionAggregate ToAggregate() => new(PresentMs, NotDetectedMs, UnknownMs, FacingMs, AwayMs, ValidSamples);
}
public sealed record ActivityResponse(Guid Id, Guid? ClientEventId, DateTimeOffset StartedAt, DateTimeOffset EndedAt,
    string? ActiveProcessName, int KeyboardEventCount, int MouseClickCount, int WindowSwitchCount, int IdleSeconds)
{
    public static ActivityResponse From(ActivityAggregate x) =>
        new(x.Id, x.ClientEventId, x.StartedAt, x.EndedAt, x.ActiveProcessName, x.KeyboardEventCount,
            x.MouseClickCount, x.WindowSwitchCount, x.IdleSeconds);
}

public sealed record EstimateResponse(Guid Id, Guid? ActivityAggregateId, DateTimeOffset StartedAt, DateTimeOffset EndedAt,
    FocusStateType EstimatedState, string RuleVersion, string? Reason, ProcessCategory? InputProcessCategory)
{
    public static EstimateResponse From(FocusStateRecord x) =>
        new(x.Id, x.ActivityAggregateId, x.StartedAt, x.EndedAt, x.State, x.RuleVersion, x.Reason, x.InputProcessCategory);
}

public sealed record CurrentStateResponse(SessionStatus SessionStatus, EstimateResponse? LatestEstimate, GoalResponse[] Goals);
public sealed record PauseResponse(DateTimeOffset StartedAt, DateTimeOffset? EndedAt);
public sealed record TimelineResponse(SessionStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt, PauseResponse[] Pauses, GoalResponse[] Goals, GoalStatistics GoalStatistics, Page<TimelineEntry> Events);
