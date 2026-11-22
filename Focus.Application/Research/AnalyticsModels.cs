using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Application.Research;

public enum ReportPeriod { Day, Week, Month }

public sealed record ReportWindow(DateTimeOffset From, DateTimeOffset To);

public sealed record ObservedActivity(Guid SessionId, DateTimeOffset StartedAt, int KeyboardEvents, int MouseClicks,
    int WindowSwitches, int IdleSeconds);

public sealed record EstimatedInterval(Guid SessionId, DateTimeOffset StartedAt, DateTimeOffset EndedAt, FocusStateType State);

public interface IAnalyticsStore
{
    IAsyncEnumerable<FocusSession> SessionsAsync(Guid owner, ReportWindow window, CancellationToken cancellationToken);
    IAsyncEnumerable<ObservedActivity> ActivityAsync(Guid owner, ReportWindow window, Guid[]? sessionIds, CancellationToken cancellationToken);
    IAsyncEnumerable<EstimatedInterval> EstimatesAsync(Guid owner, ReportWindow window, Guid[]? sessionIds, CancellationToken cancellationToken);
}

public sealed record ReportMetrics(double TrackedActiveSeconds, int Sessions, int CompletedSessions,
    double? AverageSelfReportedConcentration, double? AverageSelfReportedProductivity,
    int ConcentrationRatingCount, int ProductivityRatingCount, GoalStatistics Goals,
    long KeyboardEvents, long MouseClicks, long WindowSwitches, long ObservedIdleSeconds,
    IReadOnlyDictionary<FocusStateType, double> EstimatedStateSeconds);

public sealed record ReportBucket(DateTimeOffset From, DateTimeOffset To, ReportMetrics Metrics);
public sealed record AnalyticsReport(string TimeZone, IReadOnlyList<ReportBucket> Buckets);
public sealed record SessionResearchRow(Guid SessionId, DateTimeOffset? StartedAt, SessionStatus Status,
    double ActiveSeconds, int? SelfReportedConcentration, int? SelfReportedProductivity,
    GoalStatistics Goals, long KeyboardEvents, long MouseClicks, long WindowSwitches, long ObservedIdleSeconds,
    IReadOnlyDictionary<FocusStateType, double> EstimatedStateSeconds);
