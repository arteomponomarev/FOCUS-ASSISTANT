using Focus.Application.Common;
using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Application.Sessions;

public sealed record SessionFilter(
    PageRequest Page, DateTimeOffset? From, DateTimeOffset? To, SessionStatus? Status, bool Descending);

public interface ISessionStore
{
    Task AddAsync(FocusSession session, CancellationToken cancellationToken);
    Task<FocusSession?> FindAsync(Guid owner, Guid id, CancellationToken cancellationToken);
    Task<Page<FocusSession>> ListAsync(Guid owner, SessionFilter filter, CancellationToken cancellationToken);
    Task<T> UpdateAsync<T>(Guid owner, Guid id, Func<FocusSession, CancellationToken, Task<T>> update, CancellationToken cancellationToken);
}
