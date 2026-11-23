namespace Focus.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public User(string email, string displayName)
    {
        Id = Guid.NewGuid();
        Email = email;
        DisplayName = displayName;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? TelemetryConsentAt { get; private set; }
    public DateTimeOffset? CameraConsentAt { get; private set; }
    public bool VisionExperimental { get; private set; }

    public void Rename(string displayName) => DisplayName = displayName;

    public ICollection<FocusSession> Sessions { get; private set; }
        = new List<FocusSession>();
}
