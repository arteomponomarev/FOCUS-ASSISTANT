using System.Net;
using Focus.Domain.Enums;

namespace Focus.Agent.Sync;

internal sealed record Permission(bool TelemetryEnabled, DateTimeOffset? ConsentedAt);
internal sealed record Account(Guid Id);
internal sealed record Credentials(string AccessToken, string RefreshToken, int ExpiresIn);
internal sealed record SessionWindow(SessionStatus Status, DateTimeOffset? StartedAt, DateTimeOffset? EndedAt, PauseWindow[] Pauses);
internal sealed record PauseWindow(DateTimeOffset StartedAt, DateTimeOffset? EndedAt);
internal sealed record Authority(Permission Privacy, SessionWindow? Session, DateTimeOffset CheckedThrough);
internal sealed record Activity(Guid ClientEventId, DateTimeOffset StartedAt, DateTimeOffset EndedAt,
    string? ActiveProcessName, int KeyboardEventCount, int MouseClickCount, int WindowSwitchCount, int IdleSeconds);
internal sealed record PendingActivity(Guid SessionId, Activity Item);
internal sealed record UploadReceipt(int Created, int Duplicates);

internal sealed class RemoteFailure(HttpStatusCode status, TimeSpan? retryAfter) : Exception($"API returned HTTP {(int)status}.")
{
    public HttpStatusCode Status { get; } = status;
    public TimeSpan? RetryAfter { get; } = retryAfter;
    public bool IsTransient => Status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)Status >= 500;
}

internal interface IAgentEndpoint
{
    Task<Authority> CheckAsync(Guid session, CancellationToken ct);
    Task<UploadReceipt> UploadAsync(Guid session, IReadOnlyList<Activity> items, CancellationToken ct);
}
