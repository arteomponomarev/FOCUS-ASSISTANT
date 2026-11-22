using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Focus.Application.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

namespace Focus.Api.Hosting;

internal static class AccountAuthentication
{
    public const string Scheme = BearerTokenDefaults.AuthenticationScheme;
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private const string LoginClaim = "focus_login";
    private const string LoginFamilyClaim = "focus_login_family";

    public static void AddAccounts(this IServiceCollection services, IConfiguration config, bool production)
    {
        var protection = services.AddDataProtection().SetApplicationName("FocusAssistant.Api");
        if (config["Authentication:KeyDirectory"] is { Length: > 0 } directory)
        {
            if (production && !Path.IsPathFullyQualified(directory))
                throw new InvalidOperationException("Authentication:KeyDirectory must be an absolute path in production.");
            protection.PersistKeysToFileSystem(new DirectoryInfo(directory));
            if (config["Authentication:KeyEncryptionCertificatePath"] is { Length: > 0 } path)
            {
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(path,
                    config["Authentication:KeyEncryptionCertificatePassword"],
                    X509KeyStorageFlags.EphemeralKeySet);
                protection.ProtectKeysWithCertificate(certificate);
            }
        }
        else if (production)
            throw new InvalidOperationException("Authentication:KeyDirectory must point to a private persistent key directory.");
        else
            protection.UseEphemeralDataProtectionProvider();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AccountService>();
        services.AddAuthentication(Scheme).AddBearerToken(Scheme, options =>
        {
            options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
            options.RefreshTokenExpiration = RefreshLifetime;
            options.Events.OnMessageReceived = ValidateAccessAsync;
        });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    public static ClaimsPrincipal Principal(Guid user, Guid login, Guid family) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.ToString()),
                new Claim(LoginClaim, login.ToString()),
                new Claim(LoginFamilyClaim, family.ToString())
            ], Scheme));

    public static bool TryRead(ClaimsPrincipal? principal, out Guid user, out Guid login)
    {
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out user))
        {
            login = Guid.Empty;
            return false;
        }

        return Guid.TryParse(principal?.FindFirstValue(LoginClaim), out login);
    }

    public static Guid UserId(this ClaimsPrincipal principal) =>
        TryRead(principal, out var user, out _) ? user : throw AccountService.InvalidCredentials();

    public static Guid FamilyId(this ClaimsPrincipal principal, Guid fallback) =>
        Guid.TryParse(principal.FindFirstValue(LoginFamilyClaim), out var family) ? family : fallback;

    private static async Task ValidateAccessAsync(MessageReceivedContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.NoResult();
            return;
        }

        var ticket = context.Options.BearerTokenProtector.Unprotect(authorization[7..].Trim());
        var now = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
        if (ticket?.Properties.ExpiresUtc is not { } expires || expires <= now ||
            !TryRead(ticket.Principal, out var user, out var login) ||
            !await context.HttpContext.RequestServices.GetRequiredService<AccountService>()
                .IsActiveAsync(user, login, context.HttpContext.RequestAborted))
        {
            context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Focus.Authentication").LogWarning("Access credential was rejected");
            context.Fail("Invalid or expired credential.");
            return;
        }

        context.Principal = ticket.Principal;
        context.Properties = ticket.Properties;
        context.Success();
    }
}
