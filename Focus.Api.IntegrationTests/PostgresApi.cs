using Focus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Focus.Api.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("FOCUS_REQUIRE_POSTGRES") != "1" &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FOCUS_TEST_DATABASE")))
            Skip = "Set FOCUS_TEST_DATABASE to an isolated PostgreSQL test server.";
    }
}

internal sealed class PostgresApi : WebApplicationFactory<Program>
{
    private readonly string _connection;
    public MutableClock Clock { get; } = new();

    public PostgresApi() : this(TestConnection()) { }

    private PostgresApi(string connection) => _connection = connection;

    public PostgresApi Reopen() => new(_connection);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Contract");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = _connection,
                ["Http:AllowedOrigins:0"] = "https://focus.test"
            }));
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    public async Task MigrateAsync(string? target = null)
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FocusDbContext>().Database.MigrateAsync(target);
    }

    private static string TestConnection()
    {
        var input = Environment.GetEnvironmentVariable("FOCUS_TEST_DATABASE")
            ?? throw new InvalidOperationException("FOCUS_TEST_DATABASE is required.");
        var builder = new NpgsqlConnectionStringBuilder(input);
        if (builder.Host is not ("127.0.0.1" or "localhost" or "::1") ||
            builder.Database?.StartsWith("focus_test_", StringComparison.Ordinal) != true)
            throw new InvalidOperationException("Tests require a loopback PostgreSQL test server with a focus_test_ database prefix.");

        builder.Database = $"focus_test_{Guid.NewGuid():N}";
        return builder.ConnectionString;
    }
}

internal sealed class MutableClock : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now += delta;
}
