using Focus.Api.Hosting;
using Focus.Application.Sessions;
using Focus.Application.Research;
using Focus.Application.Settings;
using Focus.Engine;
using Focus.Infrastructure;

if (args is ["--probe-ready"])
{
    Environment.ExitCode = await LocalHealthProbe.IsReadyAsync() ? 0 : 1;
    return;
}

var migrate = args is ["--migrate"];
var builder = WebApplication.CreateBuilder(migrate ? [] : args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Services.AddInfrastructure();
if (migrate)
{
    await using var migrationHost = builder.Build();
    await DatabaseMigration.ApplyAsync(migrationHost.Services);
    return;
}

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_000_000);
var behindProxy = ProductionTransport.Configure(builder.Services, builder.Configuration);
builder.Services.AddApiHosting();
builder.Services.AddApiObservability(builder.Configuration);
builder.Services.AddAccounts(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddScoped<VisionResearch>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddSingleton<FocusClassifier>();

var app = builder.Build();
if (behindProxy)
    app.UseForwardedHeaders();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Contract"))
{
    app.MapOpenApi().AllowAnonymous();
}

app.Use(async (context, next) =>
{
    var traceId = ApiHosting.TraceId(context);
    using var scope = app.Logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId });
    context.Response.Headers["X-Request-Id"] = traceId;
    await next(context);
});
app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
app.UseStatusCodePages(async status =>
    await Results.Problem(statusCode: status.HttpContext.Response.StatusCode).ExecuteAsync(status.HttpContext));
if (app.Environment.IsProduction())
    ProductionTransport.RequireHttps(app);
if (app.Environment.IsDevelopment())
    app.UseDevelopmentOpenApiUi();

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealth();
app.MapControllers();

app.Logger.LogInformation("Focus Assistant API starting in {Environment}", app.Environment.EnvironmentName);
app.Run();

public partial class Program
{
}
