using Focus.Application.Common;
using Focus.Application.Sessions;
using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Application.Research;

public sealed class AnalyticsService(IAnalyticsStore source, SessionService sessions, TimeProvider clock)
{
    public async Task<AnalyticsReport> GetReportAsync(Guid owner, DateTimeOffset? from, DateTimeOffset? to,
        string? timeZone, ReportPeriod? period, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var zone = ResolveZone(timeZone ?? "UTC");
        var window = ValidateWindow(from, to, now);
        var buckets = Split(window, zone, period).Select(w => new Accumulator(w)).ToArray();

        await foreach (var session in source.SessionsAsync(owner, window, ct).WithCancellation(ct))
        {
            var anchor = session.StartedAt ?? session.CreatedAt;
            foreach (var bucket in buckets)
            {
                bucket.ActiveSeconds += session.GetDurations(bucket.Window.From, bucket.Window.To, now).Active.TotalSeconds;
                if (bucket.Contains(anchor))
                    bucket.CountSession(session);
            }
        }

        await foreach (var activity in source.ActivityAsync(owner, window, null, ct).WithCancellation(ct))
            Array.Find(buckets, x => x.Contains(activity.StartedAt))?.CountActivity(activity);

        await foreach (var interval in UncoveredEstimates(owner, window, null, ct))
            foreach (var bucket in buckets)
                bucket.CountEstimate(interval);

        return new AnalyticsReport(timeZone ?? "UTC", buckets.Select(x => x.Build()).ToArray());
    }

    public async Task<Page<SessionResearchRow>> ResearchAsync(Guid owner, SessionFilter filter, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var page = await sessions.ListAsync(owner, filter, ct);
        if (page.Items.Count == 0)
            return new Page<SessionResearchRow>([], page.PageNumber, page.PageSize, page.TotalCount);

        var extent = new ReportWindow(page.Items.Min(x => x.StartedAt ?? x.CreatedAt), now.AddTicks(10));
        var bySession = page.Items.ToDictionary(x => x.Id, _ => new Accumulator(extent));
        var ids = bySession.Keys.ToArray();
        await foreach (var activity in source.ActivityAsync(owner, extent, ids, ct).WithCancellation(ct))
            bySession[activity.SessionId].CountActivity(activity);

        await foreach (var estimate in UncoveredEstimates(owner, extent, ids, ct))
            bySession[estimate.SessionId].CountEstimate(estimate);

        return page.Select(session =>
        {
            var metrics = bySession[session.Id].Build().Metrics;
            return new SessionResearchRow(session.Id, session.StartedAt, session.Status,
                session.GetDurations(now).Active.TotalSeconds, session.SelfReportedConcentration, session.SelfReportedProductivity,
                GoalStatistics.From(session.Goals), metrics.KeyboardEvents, metrics.MouseClicks, metrics.WindowSwitches,
                metrics.ObservedIdleSeconds, metrics.EstimatedStateSeconds);
        });
    }

    private async IAsyncEnumerable<EstimatedInterval> UncoveredEstimates(Guid owner, ReportWindow window, Guid[]? ids,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        Guid? session = null;
        var coveredUntil = window.From;
        // The store orders by session, start time, then record ID.
        await foreach (var estimate in source.EstimatesAsync(owner, window, ids, ct).WithCancellation(ct))
        {
            if (session != estimate.SessionId)
            {
                session = estimate.SessionId;
                coveredUntil = window.From;
            }

            var start = estimate.StartedAt > coveredUntil ? estimate.StartedAt : coveredUntil;
            var end = estimate.EndedAt < window.To ? estimate.EndedAt : window.To;
            if (start >= end)
                continue;

            coveredUntil = end;
            yield return estimate with { StartedAt = start, EndedAt = end };
        }
    }

    private static ReportWindow ValidateWindow(DateTimeOffset? from, DateTimeOffset? to, DateTimeOffset now)
    {
        var end = (to ?? now).ToUniversalTime();
        if (end.Year is < 2000 or > 2100)
            throw new UseCaseException(Failure.Invalid, "The report end year is outside the supported range.");

        var start = (from ?? end.AddDays(-30)).ToUniversalTime();
        if (start >= end || end - start > TimeSpan.FromDays(366))
            throw new UseCaseException(Failure.Invalid, "Provide a positive report range up to 366 days.");

        return new ReportWindow(start, end);
    }

    private static TimeZoneInfo ResolveZone(string id)
    {
        try
        {
            if (id != "UTC" && !TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out _))
                throw new TimeZoneNotFoundException();

            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new UseCaseException(Failure.Invalid, "Use a recognized IANA reporting time zone.");
        }
    }

    private static IEnumerable<ReportWindow> Split(ReportWindow window, TimeZoneInfo zone, ReportPeriod? period)
    {
        if (period is null)
        {
            yield return window;
            yield break;
        }

        var day = TimeZoneInfo.ConvertTime(window.From, zone).Date;
        var calendar = period switch
        {
            ReportPeriod.Day => day,
            ReportPeriod.Week => day.AddDays(-((int)day.DayOfWeek + 6) % 7),
            ReportPeriod.Month => day.AddDays(1 - day.Day),
            _ => throw new UseCaseException(Failure.Invalid, "Invalid report period.")
        };

        var start = window.From;
        while (start < window.To)
        {
            calendar = period switch
            {
                ReportPeriod.Day => calendar.AddDays(1),
                ReportPeriod.Week => calendar.AddDays(7),
                _ => calendar.AddMonths(1)
            };
            var boundary = AtBoundary(calendar, zone);
            if (boundary <= start)
                continue;
            var end = boundary < window.To ? boundary : window.To;
            yield return new ReportWindow(start, end);
            start = end;
        }
    }

    private static DateTimeOffset AtBoundary(DateTime local, TimeZoneInfo zone)
    {
        while (zone.IsInvalidTime(local))
            local = local.AddMinutes(1);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    private sealed class Accumulator(ReportWindow window)
    {
        private int _sessions, _completed, _goals, _completeGoals, _partialGoals;
        private int _concentrationCount, _productivityCount;
        private long _concentration, _productivity, _keyboard, _mouse, _switches, _idle;
        private readonly Dictionary<FocusStateType, double> _states = Enum.GetValues<FocusStateType>().ToDictionary(x => x, _ => 0d);

        public ReportWindow Window { get; } = window;
        public double ActiveSeconds { get; set; }

        public bool Contains(DateTimeOffset instant) => instant >= Window.From && instant < Window.To;

        public void CountSession(FocusSession session)
        {
            _sessions++;
            if (session.Status == SessionStatus.Completed)
                _completed++;
            if (session.SelfReportedConcentration is { } concentration)
            {
                _concentration += concentration;
                _concentrationCount++;
            }
            if (session.SelfReportedProductivity is { } productivity)
            {
                _productivity += productivity;
                _productivityCount++;
            }

            var goals = GoalStatistics.From(session.Goals);
            _goals += goals.Total;
            _completeGoals += goals.Completed;
            _partialGoals += goals.PartiallyCompleted;
        }

        public void CountActivity(ObservedActivity activity)
        {
            _keyboard += activity.KeyboardEvents;
            _mouse += activity.MouseClicks;
            _switches += activity.WindowSwitches;
            _idle += activity.IdleSeconds;
        }

        public void CountEstimate(EstimatedInterval interval)
        {
            var from = interval.StartedAt > Window.From ? interval.StartedAt : Window.From;
            var to = interval.EndedAt < Window.To ? interval.EndedAt : Window.To;
            if (from < to && _states.TryGetValue(interval.State, out var duration))
                _states[interval.State] = duration + (to - from).TotalSeconds;
        }

        public ReportBucket Build() => new(Window.From, Window.To, new ReportMetrics(ActiveSeconds, _sessions, _completed,
            _concentrationCount == 0 ? null : (double)_concentration / _concentrationCount,
            _productivityCount == 0 ? null : (double)_productivity / _productivityCount,
            _concentrationCount, _productivityCount,
            new GoalStatistics(_goals, _completeGoals, _partialGoals, _goals == 0 ? 0 : 100 * (_completeGoals + 0.5 * _partialGoals) / _goals),
            _keyboard, _mouse, _switches, _idle, _states));
    }
}
