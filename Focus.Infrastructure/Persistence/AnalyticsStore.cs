using Focus.Application.Research;
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

internal sealed class AnalyticsStore(FocusDbContext db) : IAnalyticsStore
{
    public IAsyncEnumerable<FocusSession> SessionsAsync(Guid owner, ReportWindow window, CancellationToken ct) =>
        db.FocusSessions.AsNoTracking().Where(x => x.UserId == owner &&
            ((x.StartedAt ?? x.CreatedAt) >= window.From || (x.StartedAt != null && (x.EndedAt == null || x.EndedAt > window.From))) &&
            (x.StartedAt ?? x.CreatedAt) < window.To)
            .Include(x => x.Goals).Include(x => x.Pauses).AsSplitQuery().AsAsyncEnumerable();

    public IAsyncEnumerable<ObservedActivity> ActivityAsync(Guid owner, ReportWindow window, Guid[]? ids, CancellationToken ct)
    {
        var rows = db.ActivityAggregates.AsNoTracking().Where(x => x.FocusSession.UserId == owner && x.Vision == null &&
            x.StartedAt >= window.From && x.StartedAt < window.To);

        if (ids is not null)
            rows = rows.Where(x => ids.Contains(x.FocusSessionId));

        return rows.Select(x => new ObservedActivity(x.FocusSessionId, x.StartedAt, x.KeyboardEventCount,
            x.MouseClickCount, x.WindowSwitchCount, x.IdleSeconds)).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<EstimatedInterval> EstimatesAsync(Guid owner, ReportWindow window, Guid[]? ids, CancellationToken ct)
    {
        var rows = db.FocusStateRecords.AsNoTracking().Where(x => x.FocusSession.UserId == owner &&
            x.StartedAt < window.To && x.EndedAt > window.From && x.EndedAt > x.StartedAt);

        if (ids is not null)
            rows = rows.Where(x => ids.Contains(x.FocusSessionId));

        return rows.OrderBy(x => x.FocusSessionId).ThenBy(x => x.StartedAt).ThenBy(x => x.Id)
            .Select(x => new EstimatedInterval(x.FocusSessionId, x.StartedAt, x.EndedAt, x.State)).AsAsyncEnumerable();
    }
}
