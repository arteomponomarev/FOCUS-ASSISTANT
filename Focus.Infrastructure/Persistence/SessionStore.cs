using Focus.Application.Common;
using Focus.Application.Sessions;
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Focus.Infrastructure.Persistence;

internal sealed class SessionStore(FocusDbContext db) : ISessionStore
{
    public async Task AddAsync(FocusSession session, CancellationToken ct)
    {
        if (session.WorkProfileId is { } profileId && !await db.Set<WorkProfile>()
            .AnyAsync(x => x.Id == profileId && x.UserId == session.UserId, ct))
            throw UseCaseException.NotFound();

        db.FocusSessions.Add(session);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: "FK_FocusSessions_WorkProfiles_UserId_WorkProfileId"
        })
        {
            throw UseCaseException.NotFound();
        }
    }

    public Task<FocusSession?> FindAsync(Guid owner, Guid id, CancellationToken ct) =>
        WithDetails(db.FocusSessions.AsNoTracking()).SingleOrDefaultAsync(x => x.UserId == owner && x.Id == id, ct);

    public async Task<Page<FocusSession>> ListAsync(Guid owner, SessionFilter filter, CancellationToken ct)
    {
        var query = db.FocusSessions.AsNoTracking().Where(x => x.UserId == owner);
        if (filter.From is { } from)
            query = query.Where(x => (x.StartedAt ?? x.CreatedAt) >= from);
        if (filter.To is { } to)
            query = query.Where(x => (x.StartedAt ?? x.CreatedAt) < to);
        if (filter.Status is { } status)
            query = query.Where(x => x.Status == status);

        var count = await query.LongCountAsync(ct);
        var sorted = filter.Descending
            ? query.OrderByDescending(x => x.StartedAt ?? x.CreatedAt).ThenByDescending(x => x.Id)
            : query.OrderBy(x => x.StartedAt ?? x.CreatedAt).ThenBy(x => x.Id);
        var items = await WithDetails(sorted.Skip(filter.Page.Offset).Take(filter.Page.Size)).ToListAsync(ct);
        return new Page<FocusSession>(items, filter.Page.Number, filter.Page.Size, count);
    }

    public async Task<T> UpdateAsync<T>(Guid owner, Guid id, Func<FocusSession, CancellationToken, Task<T>> update, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // All session mutations acquire the account lock before the session lock.
        var accounts = await db.Database.SqlQuery<Guid>(
            $$"""SELECT "Id" AS "Value" FROM "Users" WHERE "Id" = {{owner}} FOR SHARE""").ToListAsync(ct);
        if (accounts.Count == 0)
            throw UseCaseException.NotFound();

        var locked = await db.Database.SqlQuery<Guid>(
            $$"""SELECT "Id" AS "Value" FROM "FocusSessions" WHERE "Id" = {{id}} AND "UserId" = {{owner}} FOR UPDATE""")
            .ToListAsync(ct);
        if (locked.Count == 0)
            throw UseCaseException.NotFound();

        var session = await WithDetails(db.FocusSessions).SingleAsync(x => x.Id == id && x.UserId == owner, ct);
        var result = await update(session, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    private static IQueryable<FocusSession> WithDetails(IQueryable<FocusSession> query) =>
        query.Include(x => x.Goals).Include(x => x.Pauses).AsSplitQuery();
}
