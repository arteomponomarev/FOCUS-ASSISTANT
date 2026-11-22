namespace Focus.Domain.Entities;

public sealed class SessionPause
{
    private SessionPause()
    {
    }

    internal SessionPause(Guid sessionId, DateTimeOffset startedAt)
    {
        Id = Guid.NewGuid();
        FocusSessionId = sessionId;
        StartedAt = startedAt;
    }

    public Guid Id { get; private set; }
    public Guid FocusSessionId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    internal void End(DateTimeOffset endedAt) => EndedAt = endedAt;
}
