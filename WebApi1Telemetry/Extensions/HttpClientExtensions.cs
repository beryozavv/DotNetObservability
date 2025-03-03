using Microsoft.Extensions.Options;
using Refit;
using WebApi1Telemetry.HttpClients;
using WebApi1Telemetry.Settings;

namespace WebApi1Telemetry.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddRefitHttpClients(this IServiceCollection services)
    {
        services
            .AddRefitClient<IWebApi2TelClient>()
            .ConfigureHttpClient((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<AppSettings>>();
                client.BaseAddress = new Uri(options.Value.WebApi2TelemetryUri);
            });

        return services;
    }
}