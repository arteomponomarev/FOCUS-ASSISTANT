using Focus.Application.Common;
using Focus.Application.Research;
using Focus.Domain.Entities;
using Focus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

internal sealed class ResearchStore(FocusDbContext db) : IResearchStore
{
    public Task<DateTimeOffset?> GetConsentForUploadAsync(Guid owner, CancellationToken ct) =>
        db.Users.Where(x => x.Id == owner).Select(x => x.TelemetryConsentAt).SingleOrDefaultAsync(ct);

    public Task<Dictionary<Guid, ActivityAggregate>> FindByClientIdsAsync(Guid session, Guid[] clientIds, CancellationToken ct) =>
        db.ActivityAggregates.AsNoTracking()
            .Where(x => x.FocusSessionId == session && x.ClientEventId.HasValue && clientIds.Contains(x.ClientEventId.Value))
            .ToDictionaryAsync(x => x.ClientEventId!.Value, ct);

    public async Task<bool> OverlapsAsync(Guid session, AggregateInput[] inputs, CancellationToken ct)
    {
        var starts = inputs.Select(x => x.StartedAt).ToArray();
        var ends = inputs.Select(x => x.EndedAt).ToArray();
        var result = await db.Database.SqlQuery<bool>($$"""
            SELECT EXISTS (
                SELECT 1
                FROM unnest({{starts}}::timestamptz[], {{ends}}::timestamptz[]) AS batch(start_at, end_at)
                JOIN "ActivityAggregates" AS activity
                  ON activity."FocusSessionId" = {{session}}
                 AND activity."StartedAt" < batch.end_at AND activity."EndedAt" > batch.start_at
            ) AS "Value"
            """).ToListAsync(ct);
        return result.Single();
    }

    public Task<Dictionary<string, ProcessCategory>> GetProcessCategoriesAsync(Guid? profileId, CancellationToken ct) =>
        profileId is null
            ? Task.FromResult(new Dictionary<string, ProcessCategory>())
            : db.Set<ProcessRule>().Where(x => x.WorkProfileId == profileId)
                .ToDictionaryAsync(x => x.ProcessName, x => x.Category, ct);

    public void Add(ActivityAggregate activity, FocusStateRecord estimate)
    {
        db.ActivityAggregates.Add(activity);
        db.FocusStateRecords.Add(estimate);
    }

    public Task<Page<ActivityAggregate>> ListActivityAsync(Guid owner, Guid session, PageRequest page, CancellationToken ct) =>
        ReadPageAsync(db.ActivityAggregates.AsNoTracking().Where(x => x.FocusSessionId == session && x.FocusSession.UserId == owner)
            .OrderBy(x => x.StartedAt).ThenBy(x => x.Id), page, ct);

    public Task<Page<FocusStateRecord>> ListEstimatesAsync(Guid owner, Guid session, PageRequest page, CancellationToken ct) =>
        ReadPageAsync(db.FocusStateRecords.AsNoTracking().Where(x => x.FocusSessionId == session && x.FocusSession.UserId == owner)
            .OrderBy(x => x.StartedAt).ThenBy(x => x.Id), page, ct);

    public Task<FocusStateRecord?> GetLatestEstimateAsync(Guid owner, Guid session, CancellationToken ct) =>
        db.FocusStateRecords.AsNoTracking().Where(x => x.FocusSessionId == session && x.FocusSession.UserId == owner)
            .OrderByDescending(x => x.EndedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);

    public Task<Page<TimelineEntry>> ListTimelineAsync(Guid owner, Guid session, PageRequest page, CancellationToken ct)
    {
        var activity = db.ActivityAggregates.AsNoTracking()
            .Where(x => x.FocusSessionId == session && x.FocusSession.UserId == owner)
            .Select(x => new { x.Id, x.StartedAt, x.EndedAt, Kind = "observedActivity", EstimatedState = (FocusStateType?)null });
        var estimates = db.FocusStateRecords.AsNoTracking()
            .Where(x => x.FocusSessionId == session && x.FocusSession.UserId == owner)
            .Select(x => new { x.Id, x.StartedAt, x.EndedAt, Kind = "estimatedState", EstimatedState = (FocusStateType?)x.State });
        var timeline = activity.Concat(estimates).OrderBy(x => x.StartedAt).ThenBy(x => x.Kind).ThenBy(x => x.Id)
            .Select(x => new TimelineEntry(x.Id, x.StartedAt, x.EndedAt, x.Kind, x.EstimatedState));
        return ReadPageAsync(timeline, page, ct);
    }

    private static async Task<Page<T>> ReadPageAsync<T>(IQueryable<T> source, PageRequest page, CancellationToken ct)
    {
        var count = await source.LongCountAsync(ct);
        var items = await source.Skip(page.Offset).Take(page.Size).ToListAsync(ct);
        return new Page<T>(items, page.Number, page.Size, count);
    }
}
