using Focus.Application.Common;
using Focus.Application.Settings;
using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Focus.Infrastructure.Persistence;

internal sealed class SettingsStore(FocusDbContext db) : ISettingsStore
{
    public Task<UserPreferences?> GetPreferencesAsync(Guid owner, CancellationToken ct) =>
        db.Set<UserPreferences>().AsNoTracking().SingleOrDefaultAsync(x => x.UserId == owner, ct);

    public async Task UpdatePreferencesAsync(Guid owner, Action<UserPreferences> update, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var locked = await db.Database.SqlQuery<Guid>(
            $$"""SELECT "Id" AS "Value" FROM "Users" WHERE "Id" = {{owner}} FOR UPDATE""").ToListAsync(ct);
        if (locked.Count == 0)
            throw UseCaseException.NotFound();

        var preferences = await db.Set<UserPreferences>().SingleOrDefaultAsync(x => x.UserId == owner, ct);
        if (preferences is null)
        {
            preferences = new UserPreferences(owner);
            db.Add(preferences);
        }

        update(preferences);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<Page<WorkProfile>> ListProfilesAsync(Guid owner, PageRequest page, CancellationToken ct)
    {
        var profiles = db.Set<WorkProfile>().AsNoTracking().Where(x => x.UserId == owner);
        var count = await profiles.LongCountAsync(ct);
        var items = await profiles.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip(page.Offset).Take(page.Size).Include(x => x.Rules).ToListAsync(ct);
        return new Page<WorkProfile>(items, page.Number, page.Size, count);
    }

    public Task<WorkProfile?> GetProfileAsync(Guid owner, Guid id, CancellationToken ct) =>
        db.Set<WorkProfile>().AsNoTracking().Include(x => x.Rules).SingleOrDefaultAsync(x => x.UserId == owner && x.Id == id, ct);

    public async Task AddProfileAsync(WorkProfile profile, CancellationToken ct)
    {
        db.Add(profile);
        await db.SaveChangesAsync(ct);
    }

    public async Task<WorkProfile> UpdateProfileAsync(Guid owner, Guid id, Action<WorkProfile> update, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var locked = await db.Database.SqlQuery<Guid>(
            $$"""SELECT "Id" AS "Value" FROM "WorkProfiles" WHERE "Id" = {{id}} AND "UserId" = {{owner}} FOR UPDATE""").ToListAsync(ct);
        if (locked.Count == 0)
            throw UseCaseException.NotFound();

        var profile = await db.Set<WorkProfile>().Include(x => x.Rules).SingleAsync(x => x.Id == id && x.UserId == owner, ct);
        update(profile);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return profile;
    }

    public async Task DeleteProfileAsync(Guid owner, Guid id, CancellationToken ct)
    {
        try
        {
            if (await db.Set<WorkProfile>().Where(x => x.Id == id && x.UserId == owner).ExecuteDeleteAsync(ct) == 0)
                throw UseCaseException.NotFound();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw UseCaseException.Conflict("A session references this work profile.");
        }
    }
}
