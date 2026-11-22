using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Focus.Api.Hosting;

public sealed class ApiRuntimeMetrics
{
    public const string MeterName = "Focus.Assistant";
    private readonly Counter<long> _readinessFailures;
    private readonly Counter<long> _persistedActivity;

    public ApiRuntimeMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);
        _readinessFailures = meter.CreateCounter<long>("focus.database.readiness_failures");
        _persistedActivity = meter.CreateCounter<long>("focus.activity.persisted_windows");
    }

    public void ReadinessFailed() => _readinessFailures.Add(1);

    public void ActivityPersisted(int windows) => _persistedActivity.Add(windows);
}

internal static class ApiObservability
{
    public static void AddApiObservability(this IServiceCollection services, IConfiguration config)
    {
        services.AddMetrics();
        services.AddSingleton<ApiRuntimeMetrics>();
        if (!config.GetValue<bool>("Observability:ExportOtlp"))
            return;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("focus-assistant-api"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddMeter(ApiRuntimeMetrics.MeterName)
                .AddOtlpExporter())
            .WithTracing(traces => traces
                .AddAspNetCoreInstrumentation(options => options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health"))
                .AddProcessor(new RemoveRequestDetails())
                .AddOtlpExporter());
    }

    private sealed class RemoveRequestDetails : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity activity)
        {
            foreach (var tag in new[] { "url.path", "url.query", "url.full", "client.address", "user_agent.original" })
                activity.SetTag(tag, null);
        }
    }
}
