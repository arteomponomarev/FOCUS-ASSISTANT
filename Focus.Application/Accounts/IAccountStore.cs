using Focus.Domain.Entities;

namespace Focus.Application.Accounts;

public sealed record Account(User User, string PasswordHash);

public interface IAccountStore
{
    Task<Account?> FindForLoginAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task RegisterAsync(User user, string normalizedEmail, string hash, CancellationToken cancellationToken);
    Task<Account?> FindCredentialAsync(Guid userId, CancellationToken cancellationToken);
    Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveProfileAsync(User user, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, string currentHash, string hash, CancellationToken cancellationToken);
    Task CreateLoginAsync(Guid userId, string expectedPasswordHash, Guid loginId, DateTimeOffset now,
        DateTimeOffset expires, CancellationToken cancellationToken);
    Task<bool> IsLoginActiveAsync(Guid userId, Guid loginId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<Guid?> RotateLoginAsync(Guid userId, Guid loginId, Guid replacementId, DateTimeOffset now, DateTimeOffset expires, CancellationToken cancellationToken);
    Task RevokeLoginAsync(Guid userId, Guid familyId, CancellationToken cancellationToken);
    Task SetConsentAsync(Guid userId, DateTimeOffset? consent, CancellationToken cancellationToken);
    Task SetCameraConsentAsync(Guid userId, bool? enabled, bool? experimental, DateTimeOffset now, CancellationToken cancellationToken);
    Task DeleteAccountAsync(Guid userId, string expectedHash, CancellationToken cancellationToken);
}

public interface IAccountPassword
{
    string Hash(User user, string password);
    bool Verify(Account? account, string password);
}
