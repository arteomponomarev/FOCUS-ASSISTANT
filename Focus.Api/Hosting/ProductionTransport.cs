using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Focus.Api.Hosting;

internal static class ProductionTransport
{
    public static bool Configure(IServiceCollection services, IConfiguration config)
    {
        var addresses = config.GetSection("Http:TrustedProxies").Get<string[]>() ?? [];
        var proxies = addresses.Select(value => IPAddress.TryParse(value, out var ip)
            ? ip : throw new InvalidOperationException("Http:TrustedProxies must contain IP addresses.")).ToArray();
        if (proxies.Length == 0)
            return false;

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(proxy);
                if (proxy.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    options.KnownProxies.Add(proxy.MapToIPv6());
            }
        });
        return true;
    }

    public static void RequireHttps(WebApplication application)
    {
        application.UseHsts();
        application.Use(async (context, next) =>
        {
            if (!context.Request.IsHttps && !context.Request.Path.StartsWithSegments("/health"))
            {
                await Results.Problem(statusCode: 400, title: "HTTPS is required").ExecuteAsync(context);
                return;
            }

            await next(context);
        });
    }
}
