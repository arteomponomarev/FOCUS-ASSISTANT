using System.ComponentModel.DataAnnotations;
using Focus.Api.Hosting;
using Focus.Application.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Focus.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(500)]
[EnableRateLimiting(ApiHosting.AuthenticationRateLimit)]
public sealed class AuthController(
    AccountService accounts,
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    TimeProvider clock) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<Profile>(201)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<ActionResult<Profile>> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = await accounts.RegisterAsync(request.Email, request.Password, request.DisplayName, ct);
        return CreatedAtAction(nameof(Me), user);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AccessTokenResponse>(200)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var (user, login) = await accounts.LoginAsync(request.Email, request.Password, AccountAuthentication.RefreshLifetime, ct);
        NoStore();
        return SignIn(AccountAuthentication.Principal(user, login, login), AccountAuthentication.Scheme);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenResponse>(200)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var options = bearerOptions.Get(AccountAuthentication.Scheme);
        var ticket = options.RefreshTokenProtector.Unprotect(request.RefreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expires || expires <= clock.GetUtcNow() ||
            !AccountAuthentication.TryRead(ticket.Principal, out var user, out var login))
            throw AccountService.InvalidCredentials();

        var (replacement, family) = await accounts.RefreshAsync(user, login, options.RefreshTokenExpiration, ct);
        NoStore();
        return SignIn(AccountAuthentication.Principal(user, replacement, family), AccountAuthentication.Scheme);
    }

    [HttpPost("logout")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<IActionResult> Logout(CancellationToken ct, [FromQuery] bool stopCamera = false)
    {
        if (!AccountAuthentication.TryRead(User, out var user, out var login))
            throw AccountService.InvalidCredentials();

        if (stopCamera)
            await accounts.SetPrivacyAsync(user, null, false, null, ct);
        await accounts.LogoutAsync(user, User.FamilyId(login), ct);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType<Profile>(200)]
    public Task<Profile> Me(CancellationToken ct) => accounts.GetProfileAsync(User.UserId(), ct);

    [HttpPatch("me")]
    [ProducesResponseType<Profile>(200)]
    public Task<Profile> Rename(RenameRequest request, CancellationToken ct) =>
        accounts.RenameAsync(User.UserId(), request.DisplayName, ct);

    [HttpPost("me/change-password")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(429)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await accounts.ChangePasswordAsync(User.UserId(), request.CurrentPassword, request.NewPassword, ct);
        return NoContent();
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";
}

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Password,
    [Required, StringLength(100)] string DisplayName);

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(320)] string Email,
    [Required, StringLength(128)] string Password);

public sealed record RefreshRequest([Required, StringLength(4096)] string RefreshToken);
public sealed record RenameRequest([Required, StringLength(100)] string DisplayName);
public sealed record ChangePasswordRequest(
    [Required, StringLength(128)] string CurrentPassword,
    [Required, StringLength(128, MinimumLength = 12)] string NewPassword);
