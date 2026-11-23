using Focus.Application.Common;
using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Application.Research;

public sealed record TimelineEntry(Guid Id, DateTimeOffset StartedAt, DateTimeOffset EndedAt,
    string Kind, FocusStateType? EstimatedState);

public interface IResearchStore
{
    Task<DateTimeOffset?> GetConsentForUploadAsync(Guid owner, CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetCameraConsentAsync(Guid owner, CancellationToken ct);
    Task<bool> UsesExperimentalVisionAsync(Guid owner, CancellationToken ct);
    IAsyncEnumerable<ActivityAggregate> ReadVisionAsync(Guid owner, Guid session, CancellationToken ct);
    Task<ActivityAggregate?> FindVisionAsync(Guid owner, Guid session, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<ActivityAggregate?> FindActivityAsync(Guid owner, Guid session, Guid id, CancellationToken ct);
    Task<Dictionary<Guid, ActivityAggregate>> FindByClientIdsAsync(Guid session, Guid[] clientIds, CancellationToken cancellationToken);
    Task<bool> OverlapsAsync(Guid session, AggregateInput[] inputs, CancellationToken cancellationToken);
    Task<Dictionary<string, ProcessCategory>> GetProcessCategoriesAsync(Guid? profileId, CancellationToken cancellationToken);
    void Add(ActivityAggregate activity, FocusStateRecord? estimate);
    Task<Page<ActivityAggregate>> ListActivityAsync(Guid owner, Guid session, PageRequest page, CancellationToken cancellationToken);
    Task<Page<FocusStateRecord>> ListEstimatesAsync(Guid owner, Guid session, PageRequest page, CancellationToken cancellationToken);
    Task<FocusStateRecord?> GetLatestEstimateAsync(Guid owner, Guid session, CancellationToken cancellationToken);
    Task<Page<TimelineEntry>> ListTimelineAsync(Guid owner, Guid session, PageRequest page, CancellationToken cancellationToken);
}
