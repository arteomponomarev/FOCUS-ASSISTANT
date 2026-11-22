using System.ComponentModel.DataAnnotations;
using Focus.Api.Hosting;
using Focus.Application.Common;
using Focus.Application.Research;
using Focus.Application.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Focus.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class AnalyticsController(AnalyticsService analytics, IOptions<HttpApiOptions> options) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<ReportBucket>(200)]
    public async Task<ReportBucket> Summary([FromQuery] ReportQuery query, CancellationToken ct) =>
        (await analytics.GetReportAsync(User.UserId(), query.From, query.To, query.TimeZone, null, ct)).Buckets.Single();

    [HttpGet("statistics")]
    [ProducesResponseType<AnalyticsReport>(200)]
    public Task<AnalyticsReport> Statistics([FromQuery] StatisticsQuery query, CancellationToken ct) =>
        analytics.GetReportAsync(User.UserId(), query.From, query.To, query.TimeZone, query.Period, ct);

    [HttpGet("research/sessions")]
    [ProducesResponseType<Page<SessionResearchRow>>(200)]
    public Task<Page<SessionResearchRow>> Research([FromQuery] SessionListQuery query, CancellationToken ct) =>
        analytics.ResearchAsync(User.UserId(),
            new SessionFilter(query.GetPage(options.Value.MaximumPageSize), query.From, query.To, query.Status, query.Sort == "desc"), ct);
}

public class ReportQuery
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    [StringLength(80)] public string? TimeZone { get; init; }
}

public sealed class StatisticsQuery : ReportQuery
{
    public ReportPeriod Period { get; init; } = ReportPeriod.Day;
}
