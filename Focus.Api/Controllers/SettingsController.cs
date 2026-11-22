using System.ComponentModel.DataAnnotations;
using Focus.Api.Hosting;
using Focus.Application.Common;
using Focus.Application.Settings;
using Focus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Focus.Api.Controllers;

[ApiController]
[Route("api/v1/settings")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class SettingsController(SettingsService settings, IOptions<HttpApiOptions> options) : ControllerBase
{
    [HttpGet("pomodoro")]
    [ProducesResponseType<PomodoroSettings>(200)]
    public Task<PomodoroSettings> GetPomodoro(CancellationToken ct) => settings.GetPomodoroAsync(User.UserId(), ct);

    [HttpPut("pomodoro")]
    [ProducesResponseType<PomodoroSettings>(200)]
    public Task<PomodoroSettings> SetPomodoro(PomodoroSettings request, CancellationToken ct) =>
        settings.SetPomodoroAsync(User.UserId(), request, ct);

    [HttpGet("avatar")]
    [ProducesResponseType<AvatarSettings>(200)]
    public Task<AvatarSettings> GetAvatar(CancellationToken ct) => settings.GetAvatarAsync(User.UserId(), ct);

    [HttpPut("avatar")]
    [ProducesResponseType<AvatarSettings>(200)]
    public Task<AvatarSettings> SetAvatar(AvatarSettings request, CancellationToken ct) =>
        settings.SetAvatarAsync(User.UserId(), request, ct);

    [HttpGet("work-profiles")]
    [ProducesResponseType<Page<WorkProfileResponse>>(200)]
    public async Task<Page<WorkProfileResponse>> GetProfiles([FromQuery] PagingQuery query, CancellationToken ct)
    {
        var profiles = await settings.ListProfilesAsync(User.UserId(), query.GetPage(options.Value.MaximumPageSize), ct);
        return profiles.Select(WorkProfileResponse.From);
    }

    [HttpGet("work-profiles/{id}")]
    [ProducesResponseType<WorkProfileResponse>(200)]
    public async Task<WorkProfileResponse> GetProfile(Guid id, CancellationToken ct) =>
        WorkProfileResponse.From(await settings.GetProfileAsync(User.UserId(), id, ct));

    [HttpPost("work-profiles")]
    [ProducesResponseType<WorkProfileResponse>(201)]
    public async Task<ActionResult<WorkProfileResponse>> CreateProfile(WorkProfileRequest request, CancellationToken ct)
    {
        var profile = await settings.AddProfileAsync(User.UserId(), request.ToInput(), ct);
        return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, WorkProfileResponse.From(profile));
    }

    [HttpPut("work-profiles/{id}")]
    [ProducesResponseType<WorkProfileResponse>(200)]
    public async Task<WorkProfileResponse> ReplaceProfile(Guid id, WorkProfileRequest request, CancellationToken ct) =>
        WorkProfileResponse.From(await settings.SetProfileAsync(User.UserId(), id, request.ToInput(), ct));

    [HttpDelete("work-profiles/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> DeleteProfile(Guid id, CancellationToken ct)
    {
        await settings.DeleteProfileAsync(User.UserId(), id, ct);
        return NoContent();
    }
}

public sealed record WorkProfileRequest([Required, StringLength(100)] string Name, [StringLength(500)] string? Description,
    [Range(1, 240)] int FocusMinutes, [Range(1, 120)] int BreakMinutes, [MaxLength(100)] IReadOnlyList<RuleInput>? Rules)
{
    public ProfileInput ToInput() => new(Name, Description, FocusMinutes, BreakMinutes, Rules);
}

public sealed record WorkProfileResponse(Guid Id, string Name, string? Description, int FocusMinutes, int BreakMinutes, RuleInput[] Rules)
{
    public static WorkProfileResponse From(WorkProfile profile) =>
        new(profile.Id, profile.Name, profile.Description, profile.FocusMinutes, profile.BreakMinutes,
            profile.Rules.OrderBy(x => x.ProcessName).Select(x => new RuleInput(x.ProcessName, x.Category)).ToArray());
}
