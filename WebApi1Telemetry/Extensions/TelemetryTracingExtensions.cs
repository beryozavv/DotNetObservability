using MassTransit.Logging;
using MassTransit.Monitoring;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using WebApi1Telemetry.Settings;

namespace WebApi1Telemetry.Extensions;

public static class TelemetryTracingExtensions
{
    public static IServiceCollection AddMetricsAndTracing(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelemetrySettings>(configuration.GetSection(nameof(TelemetrySettings)));
        
        var telemetryOptions = configuration.GetSection(nameof(TelemetrySettings)).Get<TelemetrySettings>();
        
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation() // Инструментация для ASP.NET Core
                    .AddHttpClientInstrumentation() // Инструментация для HttpClient
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApi1Telemetry"))
                    .AddNpgsql() // Добавляет инструментацию для Npgsql
                    .AddRedisInstrumentation()
                    // ConfigureRedisInstrumentation не обязательно
                    .ConfigureRedisInstrumentation((provider, instrumentation) =>
                    {
                        var connectionMultiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
                        instrumentation.AddConnection(connectionMultiplexer);
                    })
                    .AddSource(DiagnosticHeaders.DefaultListenerName) // MassTransit
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"http://{telemetryOptions!.JaegerHost}:{telemetryOptions.JaegerPort}");
                    })
                    .AddConsoleExporter(); // Вывод трассировки в консоль (для отладки)
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation() // Метрики для ASP.NET Core
                    .AddHttpClientInstrumentation() // Метрики для HttpClient
                    .AddMeter(InstrumentationOptions.MeterName) // MassTransit Meter
                    //.AddMeter(greeterMeter.Name) todo
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApi1Telemetry"))
                    .AddPrometheusExporter(); // Экспорт метрик в Prometheus
            });

        return services;
    }
}