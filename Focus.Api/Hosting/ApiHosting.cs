using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Focus.Api.Hosting;

internal static class ApiHosting
{
    public const string AuthenticationRateLimit = "authentication";

    public static IServiceCollection AddApiHosting(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions["traceId"] = TraceId(context.HttpContext));
        services.AddExceptionHandler<ApiErrorHandler>();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(
                new ProblemDetails
                {
                    Status = 400,
                    Title = "Request validation failed",
                    Detail = "Check the required fields, request format, and value limits in the API contract.",
                    Extensions = { ["traceId"] = TraceId(_.HttpContext) }
                })
            {
                ContentTypes = { "application/problem+json" }
            };
        });

        services.AddOptions<HttpApiOptions>().BindConfiguration("Http")
            .Validate(options => options.MaximumPageSize is >= 25 and <= 500, "Http:MaximumPageSize must be between 25 and 500.")
            .Validate(options => options.AllowedOrigins.All(HttpApiOptions.ValidOrigin), "Http:AllowedOrigins must contain explicit HTTP origins.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IOptions<HttpApiOptions>>((options, http) => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(http.Value.AllowedOrigins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                .WithHeaders("Content-Type", "Authorization")
                .WithExposedHeaders("Location", "Retry-After", "X-Request-Id")));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthenticationRateLimit, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "local",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
            options.OnRejected = async (rejected, _) =>
            {
                if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    rejected.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                await Results.Problem(statusCode: 429).ExecuteAsync(rejected.HttpContext);
            };
        });

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql", tags: ["ready"], timeout: TimeSpan.FromSeconds(5));
        services.AddOpenApi("v1", OpenApiContract.Configure);
        return services;
    }

    public static void MapHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready")
        }).AllowAnonymous();
    }

    public static string TraceId(HttpContext context) =>
        Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.AllowDuplicateProperties = false;
        options.NumberHandling = JsonNumberHandling.Strict;
    }
}
