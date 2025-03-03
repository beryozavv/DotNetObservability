using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WebApi1Telemetry.Settings;

namespace WebApi1Telemetry.Extensions;

public static class StackExchangeRedisExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisSettings>(configuration.GetSection(nameof(RedisSettings)));
        
        // Настройка распределенного кэша
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            var options = new ConfigurationOptions
            {
                EndPoints = { $"{settings.Host}:{settings.Port}" },
                AbortOnConnectFail =
                    settings.AbortOnConnectFail, // Необязательно: настройка для автоматического переподключения
                ConnectTimeout = settings.ConnectTimeout, // Таймаут подключения
                SyncTimeout = settings.SyncTimeout, // Таймаут синхронных операций
            };
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<IDistributedCache>(provider =>
        {
            var connectionMultiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
            var options = new RedisCacheOptions
            {
                ConnectionMultiplexerFactory = () =>
                {
                    IConnectionMultiplexer multiplexer = connectionMultiplexer;
                    return Task.FromResult(multiplexer);
                }
            };

            return new RedisCache(options);
        });

        // IServiceProvider not supported here
        // services.AddStackExchangeRedisCache(options =>
        // {
        //          
        //     options.Configuration = $"{(env == "Development" ? "localhost" : "redis")}:6379";
        //     options.ConnectionMultiplexerFactory = () =>
        //     {
        //         IConnectionMultiplexer multiplexer = redisConnection;
        //         return Task.FromResult(multiplexer);
        //     };
        //     options.InstanceName = "SampleInstance";
        // });

        return services;
    }
}