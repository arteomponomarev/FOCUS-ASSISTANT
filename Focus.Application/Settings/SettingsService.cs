using Focus.Application.Common;
using Focus.Application.Research;
using Focus.Domain.Entities;
using Focus.Domain.Enums;

namespace Focus.Application.Settings;

public sealed record RuleInput(string ProcessName, ProcessCategory Category);
public sealed record ProfileInput(string Name, string? Description, int FocusMinutes, int BreakMinutes, IReadOnlyList<RuleInput>? Rules);

public sealed class SettingsService(ISettingsStore store)
{
    public async Task<PomodoroSettings> GetPomodoroAsync(Guid owner, CancellationToken ct) =>
        (await store.GetPreferencesAsync(owner, ct))?.GetPomodoro() ?? PomodoroSettings.Default;

    public async Task<PomodoroSettings> SetPomodoroAsync(Guid owner, PomodoroSettings input, CancellationToken ct)
    {
        if (input.FocusMinutes is < 1 or > 240 || input.ShortBreakMinutes is < 1 or > 120 ||
            input.LongBreakMinutes is < 1 or > 120 || input.IntervalsBeforeLongBreak is < 1 or > 12 ||
            input.LongBreakMinutes < input.ShortBreakMinutes)
            throw new UseCaseException(Failure.Invalid, "Invalid durations or long-break interval.");

        await store.UpdatePreferencesAsync(owner, preferences => preferences.SetPomodoro(input), ct);
        return input;
    }

    public async Task<AvatarSettings> GetAvatarAsync(Guid owner, CancellationToken ct) =>
        (await store.GetPreferencesAsync(owner, ct))?.GetAvatar() ?? AvatarSettings.Default;

    public async Task<AvatarSettings> SetAvatarAsync(Guid owner, AvatarSettings input, CancellationToken ct)
    {
        var id = input.AvatarId?.Trim();
        if (!Enum.IsDefined(input.Visibility) || !Enum.IsDefined(input.Intensity) ||
            (id is not null && (id.Length is < 1 or > 80 ||
            !id.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))))
            throw new UseCaseException(Failure.Invalid, "Invalid avatar preferences.");

        var value = input with { AvatarId = id };
        await store.UpdatePreferencesAsync(owner, preferences => preferences.SetAvatar(value), ct);
        return value;
    }

    public Task<Page<WorkProfile>> ListProfilesAsync(Guid owner, PageRequest page, CancellationToken ct) =>
        store.ListProfilesAsync(owner, page, ct);

    public async Task<WorkProfile> GetProfileAsync(Guid owner, Guid id, CancellationToken ct) =>
        await store.GetProfileAsync(owner, id, ct) ?? throw UseCaseException.NotFound();

    public async Task<WorkProfile> AddProfileAsync(Guid owner, ProfileInput input, CancellationToken ct)
    {
        var rules = ValidateProfile(input);
        var profile = new WorkProfile(owner, input.Name.Trim(), input.Description?.Trim(), input.FocusMinutes, input.BreakMinutes);
        profile.ReplaceRules(rules);
        await store.AddProfileAsync(profile, ct);
        return profile;
    }

    public Task<WorkProfile> SetProfileAsync(Guid owner, Guid id, ProfileInput input, CancellationToken ct)
    {
        var rules = ValidateProfile(input);
        return store.UpdateProfileAsync(owner, id, profile =>
        {
            profile.Rename(input.Name.Trim(), input.Description?.Trim(), input.FocusMinutes, input.BreakMinutes);
            profile.ReplaceRules(rules);
        }, ct);
    }

    public Task DeleteProfileAsync(Guid owner, Guid id, CancellationToken ct) => store.DeleteProfileAsync(owner, id, ct);

    private static Dictionary<string, ProcessCategory> ValidateProfile(ProfileInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100 || input.Name.Any(char.IsControl) ||
            input.Description?.Length > 500 || input.Description?.Contains('\0') == true ||
            input.FocusMinutes is < 1 or > 240 || input.BreakMinutes is < 1 or > 120 || input.Rules?.Count > 100)
            throw new UseCaseException(Failure.Invalid, "Invalid work profile properties.");

        var rules = new Dictionary<string, ProcessCategory>(StringComparer.Ordinal);
        foreach (var rule in input.Rules ?? [])
        {
            if (rule is null || !ProcessIdentity.TryNormalize(rule.ProcessName, out var name) || name is null ||
                !Enum.IsDefined(rule.Category) || !rules.TryAdd(name, rule.Category))
                throw new UseCaseException(Failure.Invalid, "Use distinct valid process identities and categories.");
        }

        return rules;
    }
}
