using MassTransit;
using WebApi1Telemetry.Consumers;
using WebApi1Telemetry.Settings;

namespace WebApi1Telemetry.Extensions;

public static class MtRabbitExtensions
{
    public static void ConfigureMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection(nameof(RabbitMqSettings)));
        services.Configure<MassTransitSettings>(configuration.GetSection(nameof(MassTransitSettings)));
        
        var rabbitMqSettings = configuration.GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();
        var massTransitSettings = configuration.GetSection(nameof(MassTransitSettings)).Get<MassTransitSettings>();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            x.AddConsumer<OrderSubmittedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqSettings!.Host, rabbitMqSettings.Port, rabbitMqSettings.VirtualHost, h =>
                {
                    h.Username(rabbitMqSettings.Username);
                    h.Password(rabbitMqSettings.Password);

                    if (rabbitMqSettings.UseSsl)
                    {
                        h.UseSsl();
                    }
                });

                cfg.ReceiveEndpoint(massTransitSettings!.OrderSubmittedQueueName, e =>
                {
                    e.ConfigureConsumer<OrderSubmittedConsumer>(context);
                    e.PrefetchCount = massTransitSettings.PrefetchCount;
                    e.ConcurrentMessageLimit = massTransitSettings.ConcurrentMessageLimit;
                    e.UseMessageRetry(r => r.Interval(
                        massTransitSettings.Retry.RetryCount,
                        TimeSpan.FromSeconds(massTransitSettings.Retry.RetryIntervalSeconds)
                    ));
                });
            });
        });
    }
}