using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Focus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException(
                "PostgreSQL connection string is not configured.");

        services.AddDbContext<FocusDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}