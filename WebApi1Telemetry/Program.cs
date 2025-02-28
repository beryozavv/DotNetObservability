using MassTransit;
using MassTransit.Logging;
using MassTransit.Monitoring;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Refit;
using WebApi1Telemetry;
using WebApi1Telemetry.Data;
using WebApi1Telemetry.HttpClients;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;
using WebApi1Telemetry.Consumers;
using WebApi1Telemetry.Services;
using WebApi1Telemetry.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
});
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(nameof(RabbitMqSettings)));
builder.Services.Configure<MassTransitSettings>(builder.Configuration.GetSection(nameof(MassTransitSettings)));

builder.Services.AddTransient<IOrderService, OrderService>();

// Добавление CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


var env = builder.Environment.EnvironmentName;
builder.Configuration.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

// Настройка Redis
var redisConnection = ConnectionMultiplexer.Connect($"{(env == "Development" ? "localhost" : "redis")}:6379");
// Регистрация ConnectionMultiplexer todo
// builder.Services.AddSingleton<IConnectionMultiplexer>(sp => //todo
// {
//     var configuration = ConfigurationOptions.Parse($"{(env == "Development" ? "localhost" : "redis")}:6379"); // Укажите вашу строку подключения
//     configuration.AbortOnConnectFail = false; // Необязательно: настройка для автоматического переподключения
//     configuration.ConnectTimeout = 5000; // Таймаут подключения
//     configuration.SyncTimeout = 5000; // Таймаут синхронных операций
//
//     return ConnectionMultiplexer.Connect(configuration);
// });
// Настройка распределенного кэша (например, Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{(env == "Development" ? "localhost" : "redis")}:6379";
    options.ConnectionMultiplexerFactory = () =>
    {
        IConnectionMultiplexer multiplexer = redisConnection;
        return Task.FromResult(multiplexer);
    };
    options.InstanceName = "SampleInstance";
});


builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddRefitClient<IWebApi2TelClient>()
    .ConfigureHttpClient((provider, client) =>
    {
        var options = provider.GetRequiredService<IOptions<AppSettings>>();
        client.BaseAddress = new Uri(options.Value.WebApi2TelemetryUri);
    });


// Добавление OpenTelemetry для трассировки
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation() // Инструментация для ASP.NET Core
            .AddHttpClientInstrumentation() // Инструментация для HttpClient
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApi1Telemetry"))
            .AddNpgsql() // Добавляет инструментацию для Npgsql
            .AddRedisInstrumentation(redisConnection)
            .AddSource(DiagnosticHeaders.DefaultListenerName) // MassTransit
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://jaeger:4317"); // gRPC
            })
            .AddConsoleExporter(); // Вывод трассировки в консоль (для отладки)
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation() // Метрики для ASP.NET Core
            .AddHttpClientInstrumentation() // Метрики для HttpClient
            .AddMeter(InstrumentationOptions.MeterName) // MassTransit Meter
            //.AddRuntimeInstrumentation()
            //.AddMeter(greeterMeter.Name)
            // Metrics provides by ASP.NET Core in .NET 8
            // .AddMeter("Microsoft.AspNetCore.Hosting")
            // .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            // Metrics provided by System.Net libraries
            // .AddMeter("System.Net.Http")
            // .AddMeter("System.Net.NameResolution")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApi1Telemetry"))
            .AddPrometheusExporter(); // Экспорт метрик в Prometheus
    });

var rabbitMqSettings = builder.Configuration.GetSection(nameof(RabbitMqSettings)).Get<RabbitMqSettings>();
var massTransitSettings = builder.Configuration.GetSection(nameof(MassTransitSettings)).Get<MassTransitSettings>();


// Добавляем MassTransit в DI
builder.Services.AddMassTransit(x =>
{
    // Важно: убедитесь, что включен пропагатор OpenTelemetry
    x.SetKebabCaseEndpointNameFormatter();
    // Регистрируем потребителя
    x.AddConsumer<OrderSubmittedConsumer>();

    // Настройка транспорта (RabbitMQ) с использованием параметров из конфигурации
    x.UsingRabbitMq((context, cfg) =>
    {
        // cfg.UsePublishFilter(typeof(InstrumentationPublishFilter<>), context);
        // cfg.UseSendFilter(typeof(InstrumentationSendFilter<>), context);
        // cfg.UseConsumeFilter(typeof(InstrumentationConsumeFilter<>), context);

        // Настройка хоста используя параметры из appsettings.json
        cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.Port, rabbitMqSettings.VirtualHost, h =>
        {
            h.Username(rabbitMqSettings.Username);
            h.Password(rabbitMqSettings.Password);

            if (rabbitMqSettings.UseSSL)
            {
                h.UseSsl(s => { });
            }
        });

        // Настройка получения (Receive Endpoint)
        cfg.ReceiveEndpoint(massTransitSettings.OrderSubmittedQueueName, e =>
        {
            // Настройка потребителя
            e.ConfigureConsumer<OrderSubmittedConsumer>(context);

            // Настройка параллельной обработки
            e.PrefetchCount = massTransitSettings.PrefetchCount;
            e.ConcurrentMessageLimit = massTransitSettings.ConcurrentMessageLimit;

            // Настройка повторных попыток при ошибке
            e.UseMessageRetry(r => r.Interval(
                massTransitSettings.Retry.RetryCount,
                TimeSpan.FromSeconds(massTransitSettings.Retry.RetryIntervalSeconds)
            ));

            // Настройка обработки ошибок (опционально)
            //e.UseEntityFrameworkOutbox<ApiDbContext>(context);
        });
    });
});

var app = builder.Build();

// Добавление эндпоинта для Prometheus
app.MapPrometheusScrapingEndpoint();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment()) // todo
// {
app.UseSwagger();
app.UseSwaggerUI();
// }

app.UseHttpsRedirection();

// Использование CORS
app.UseCors("AllowAll");

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};
app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

// Команда для засеивания данных
if (true)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApiDbContext>();

    await SeedData.Initialize(context);
    Console.WriteLine("Data seeded successfully.");
}

app.Run();

namespace WebApi1Telemetry
{
    public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}