using Focus.Application.Accounts;
using Focus.Application.Common;
using Focus.Domain.Entities;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Focus.Infrastructure.Accounts;

internal sealed class AccountStore(FocusDbContext db) : IAccountStore
{
    public async Task<Account?> FindForLoginAsync(string email, CancellationToken ct)
    {
        var credential = await db.Set<AccountCredential>().AsNoTracking()
            .Include(x => x.User).SingleOrDefaultAsync(x => x.NormalizedEmail == email, ct);
        return credential is null ? null : new Account(credential.User, credential.PasswordHash);
    }

    public async Task RegisterAsync(User user, string email, string hash, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.Email.ToLower() == email, ct))
            throw DuplicateAccount();

        db.Add(new AccountCredential { UserId = user.Id, User = user, NormalizedEmail = email, PasswordHash = hash });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Users_Email" or "IX_AccountCredentials_NormalizedEmail"
        })
        {
            throw DuplicateAccount();
        }
    }

    public async Task<Account?> FindCredentialAsync(Guid userId, CancellationToken ct)
    {
        var credential = await db.Set<AccountCredential>().AsNoTracking()
            .Include(x => x.User).SingleOrDefaultAsync(x => x.UserId == userId, ct);
        return credential is null ? null : new Account(credential.User, credential.PasswordHash);
    }

    public Task<User?> FindUserAsync(Guid userId, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);

    public async Task SaveProfileAsync(User user, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw UseCaseException.NotFound();
        }
    }

    public async Task ChangePasswordAsync(Guid userId, string currentHash, string hash, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var updated = await db.Set<AccountCredential>().Where(x => x.UserId == userId && x.PasswordHash == currentHash)
            .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.PasswordHash, hash), ct);

        if (updated != 1)
            throw AccountService.InvalidCredentials();

        await db.Set<AccountLogin>().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task CreateLoginAsync(Guid userId, string expectedPasswordHash, Guid loginId, DateTimeOffset now,
        DateTimeOffset expires, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var credential = await LockCredentialAsync(userId, ct);
        if (credential?.PasswordHash != expectedPasswordHash)
            throw AccountService.InvalidCredentials();

        await db.Set<AccountLogin>().Where(x => x.UserId == userId && x.ExpiresAt <= now).ExecuteDeleteAsync(ct);
        db.Add(new AccountLogin { Id = loginId, UserId = userId, ExpiresAt = expires, FamilyId = loginId });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public Task<bool> IsLoginActiveAsync(Guid userId, Guid loginId, DateTimeOffset now, CancellationToken ct) =>
        db.Set<AccountLogin>().AnyAsync(x => x.UserId == userId && x.Id == loginId && x.ExpiresAt > now, ct);

    public async Task<Guid?> RotateLoginAsync(Guid userId, Guid loginId, Guid replacementId, DateTimeOffset now, DateTimeOffset expires, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await LockCredentialAsync(userId, ct) is null)
            return null;

        var login = await db.Set<AccountLogin>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Id == loginId && x.ExpiresAt > now, ct);
        if (login is null)
            return null;

        var family = login.FamilyId ?? login.Id;
        await db.Set<AccountLogin>().Where(x => x.Id == login.Id).ExecuteDeleteAsync(ct);
        db.Add(new AccountLogin { Id = replacementId, UserId = userId, ExpiresAt = expires, FamilyId = family });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return family;
    }

    public async Task RevokeLoginAsync(Guid userId, Guid familyId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await LockCredentialAsync(userId, ct) is null)
            return;

        await db.Set<AccountLogin>().Where(x => x.UserId == userId && (x.FamilyId == familyId || x.Id == familyId))
            .ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task SetConsentAsync(Guid userId, DateTimeOffset? consent, CancellationToken ct)
    {
        var updated = await db.Users.Where(x => x.Id == userId).ExecuteUpdateAsync(updates =>
            updates.SetProperty(x => x.TelemetryConsentAt, x => consent == null ? null : x.TelemetryConsentAt ?? consent), ct);
        if (updated == 0)
            throw UseCaseException.NotFound();
    }

    public async Task DeleteAccountAsync(Guid userId, string expectedHash, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var credential = await LockCredentialAsync(userId, ct);
        if (credential?.PasswordHash != expectedHash)
            throw AccountService.InvalidCredentials();

        await db.Users.Where(x => x.Id == userId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task<AccountCredential?> LockCredentialAsync(Guid userId, CancellationToken ct) =>
        (await db.Set<AccountCredential>().FromSqlInterpolated(
            $$"""SELECT * FROM "AccountCredentials" WHERE "UserId" = {{userId}} FOR UPDATE""")
            .AsNoTracking().ToListAsync(ct)).SingleOrDefault();

    private static UseCaseException DuplicateAccount() =>
        UseCaseException.Conflict("An account with this email already exists.");
}
