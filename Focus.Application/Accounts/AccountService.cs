using Focus.Application.Common;
using Focus.Domain.Entities;

namespace Focus.Application.Accounts;

public sealed record Profile(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt);
public sealed record PrivacySettings(bool TelemetryEnabled, DateTimeOffset? ConsentedAt);

public sealed class AccountService(IAccountStore store, IAccountPassword passwords, TimeProvider clock)
{
    public async Task<Profile> RegisterAsync(string email, string password, string displayName, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = new User(normalized, CleanDisplayName(displayName));
        await store.RegisterAsync(user, normalized, passwords.Hash(user, password), ct);
        return Project(user);
    }

    public async Task<(Guid UserId, Guid LoginId)> LoginAsync(string email, string password, TimeSpan lifetime, CancellationToken ct)
    {
        var account = await store.FindForLoginAsync(email.Trim().ToLowerInvariant(), ct);
        if (!passwords.Verify(account, password) || account is null)
            throw InvalidCredentials();

        var login = Guid.NewGuid();
        var now = clock.GetUtcNow();
        await store.CreateLoginAsync(account.User.Id, account.PasswordHash, login, now, now.Add(lifetime), ct);
        return (account.User.Id, login);
    }

    public async Task<(Guid LoginId, Guid FamilyId)> RefreshAsync(Guid user, Guid login, TimeSpan lifetime, CancellationToken ct)
    {
        var replacement = Guid.NewGuid();
        var now = clock.GetUtcNow();
        var family = await store.RotateLoginAsync(user, login, replacement, now, now.Add(lifetime), ct)
            ?? throw InvalidCredentials();
        return (replacement, family);
    }

    public Task LogoutAsync(Guid user, Guid family, CancellationToken ct) =>
        store.RevokeLoginAsync(user, family, ct);

    public Task<bool> IsActiveAsync(Guid user, Guid login, CancellationToken ct) =>
        store.IsLoginActiveAsync(user, login, clock.GetUtcNow(), ct);

    public async Task<Profile> GetProfileAsync(Guid id, CancellationToken ct) =>
        Project(await store.FindUserAsync(id, ct) ?? throw UseCaseException.NotFound());

    public async Task<PrivacySettings> GetPrivacyAsync(Guid id, CancellationToken ct)
    {
        var user = await store.FindUserAsync(id, ct) ?? throw UseCaseException.NotFound();
        return new PrivacySettings(user.TelemetryConsentAt.HasValue, user.TelemetryConsentAt);
    }

    public async Task<PrivacySettings> SetPrivacyAsync(Guid id, bool enabled, CancellationToken ct)
    {
        await store.SetConsentAsync(id, enabled ? clock.GetUtcNow() : null, ct);
        return await GetPrivacyAsync(id, ct);
    }

    public async Task<Profile> RenameAsync(Guid id, string displayName, CancellationToken ct)
    {
        var user = await store.FindUserAsync(id, ct) ?? throw UseCaseException.NotFound();
        user.Rename(CleanDisplayName(displayName));
        await store.SaveProfileAsync(user, ct);
        return Project(user);
    }

    public async Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken ct)
    {
        var account = await store.FindCredentialAsync(id, ct);
        if (!passwords.Verify(account, currentPassword) || account is null)
            throw InvalidCredentials();

        await store.ChangePasswordAsync(id, account.PasswordHash, passwords.Hash(account.User, newPassword), ct);
    }

    public async Task DeleteAccountAsync(Guid id, string password, CancellationToken ct)
    {
        var account = await store.FindCredentialAsync(id, ct);
        if (!passwords.Verify(account, password) || account is null)
            throw InvalidCredentials();

        await store.DeleteAccountAsync(id, account.PasswordHash, ct);
    }

    public static UseCaseException InvalidCredentials() =>
        new(Failure.Unauthenticated, "The credentials are invalid or expired.");

    private static Profile Project(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.CreatedAt);

    private static string CleanDisplayName(string value)
    {
        var name = value.Trim();
        if (name.Length is 0 or > 100 || name.Any(char.IsControl))
            throw new UseCaseException(Failure.Invalid, "Display name must contain 1 to 100 printable characters.");

        return name;
    }
}
