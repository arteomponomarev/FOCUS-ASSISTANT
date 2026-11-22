using Focus.Application.Accounts;
using Focus.Application.Sessions;
using Focus.Application.Research;
using Focus.Application.Settings;
using Focus.Infrastructure.Accounts;
using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Focus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<PostgreSqlOptions>()
            .Configure<IConfiguration>((options, config) => options.Connection = config.GetConnectionString("PostgreSQL"))
            .Validate(options => IsValid(options.Connection), "A valid ConnectionStrings:PostgreSQL configuration is required.")
            .ValidateOnStart();
        services.AddDbContext<FocusDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IOptions<PostgreSqlOptions>>().Value.Connection));
        services.AddScoped<IAccountStore, AccountStore>();
        services.AddSingleton<IAccountPassword, AccountPassword>();
        services.AddScoped<ISessionStore, SessionStore>();
        services.AddScoped<IResearchStore, ResearchStore>();
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<IAnalyticsStore, AnalyticsStore>();
        return services;
    }

    private static bool IsValid(string? connection)
    {
        if (string.IsNullOrWhiteSpace(connection))
            return false;

        try
        {
            var parsed = new NpgsqlConnectionStringBuilder(connection);
            return !string.IsNullOrWhiteSpace(parsed.Host) && !string.IsNullOrWhiteSpace(parsed.Database) &&
                !parsed.IncludeErrorDetail && !parsed.LogParameters;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private sealed class PostgreSqlOptions
    {
        public string? Connection { get; set; }
    }
}
