using Focus.Application.Common;
using Focus.Domain.Entities;

namespace Focus.Application.Settings;

public interface ISettingsStore
{
    Task<UserPreferences?> GetPreferencesAsync(Guid owner, CancellationToken cancellationToken);
    Task UpdatePreferencesAsync(Guid owner, Action<UserPreferences> update, CancellationToken cancellationToken);
    Task<Page<WorkProfile>> ListProfilesAsync(Guid owner, PageRequest page, CancellationToken cancellationToken);
    Task<WorkProfile?> GetProfileAsync(Guid owner, Guid id, CancellationToken cancellationToken);
    Task AddProfileAsync(WorkProfile profile, CancellationToken cancellationToken);
    Task<WorkProfile> UpdateProfileAsync(Guid owner, Guid id, Action<WorkProfile> update, CancellationToken cancellationToken);
    Task DeleteProfileAsync(Guid owner, Guid id, CancellationToken cancellationToken);
}
