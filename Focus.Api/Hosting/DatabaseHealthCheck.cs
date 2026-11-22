using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Focus.Api.Hosting;

internal sealed class DatabaseHealthCheck(IServiceScopeFactory scopes, ApiRuntimeMetrics metrics,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<FocusDbContext>().Database;
        try
        {
            if (await database.CanConnectAsync(cancellationToken) &&
                !(await database.GetPendingMigrationsAsync(cancellationToken)).Any())
                return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning("Database readiness check failed with exception type {ExceptionType}", exception.GetType().FullName);
        }

        metrics.ReadinessFailed();
        logger.LogWarning("Database readiness check did not pass");
        return HealthCheckResult.Unhealthy();
    }
}
