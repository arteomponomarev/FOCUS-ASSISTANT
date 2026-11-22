using System.ComponentModel.DataAnnotations;
using Focus.Api.Hosting;
using Focus.Application.Accounts;
using Focus.Application.Settings;
using Focus.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Focus.Api.Controllers;

[ApiController]
[Route("api/v1/settings/privacy")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(500)]
public sealed class PrivacyController(AccountService accounts, SettingsService settings) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PrivacySettings>(200)]
    public Task<PrivacySettings> Get(CancellationToken ct) => accounts.GetPrivacyAsync(User.UserId(), ct);

    [HttpPatch]
    [ProducesResponseType<PrivacySettings>(200)]
    public Task<PrivacySettings> Update(PrivacyUpdate request, CancellationToken ct) =>
        accounts.SetPrivacyAsync(User.UserId(), request.TelemetryEnabled, ct);

    [HttpGet("export")]
    [ProducesResponseType<PrivateExport>(200)]
    public async Task<PrivateExport> Export(CancellationToken ct)
    {
        var owner = User.UserId();
        Response.Headers.CacheControl = "no-store";
        return new PrivateExport(await accounts.GetProfileAsync(owner, ct), await accounts.GetPrivacyAsync(owner, ct),
            await settings.GetPomodoroAsync(owner, ct), await settings.GetAvatarAsync(owner, ct),
            "/api/v1/settings/work-profiles", "/api/v1/sessions");
    }

    [HttpPost("delete-account")]
    [EnableRateLimiting(ApiHosting.AuthenticationRateLimit)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken ct)
    {
        await accounts.DeleteAccountAsync(User.UserId(), request.Password, ct);
        return NoContent();
    }
}

public sealed record PrivacyUpdate([property: System.Text.Json.Serialization.JsonRequired] bool TelemetryEnabled);
public sealed record DeleteAccountRequest([Required, StringLength(128)] string Password);
public sealed record PrivateExport(Profile Profile, PrivacySettings Privacy, PomodoroSettings Pomodoro,
    AvatarSettings Avatar, string WorkProfilesPath, string SessionsPath);
