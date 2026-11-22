using Focus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Focus.Api.Hosting;

internal static class DatabaseMigration
{
    public static async Task ApplyAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<FocusDbContext>().Database;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Focus.DatabaseMigration");
        try
        {
            await database.MigrateAsync();
            logger.LogInformation("Database migrations are up to date");
        }
        catch (Exception exception)
        {
            logger.LogError("Migration job failed with exception type {ExceptionType}", exception.GetType().FullName);
            Environment.ExitCode = 1;
        }
    }
}
