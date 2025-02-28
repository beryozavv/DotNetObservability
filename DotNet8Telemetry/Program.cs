// See https://aka.ms/new-console-template for more information

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// builder.WebHost.ConfigureKestrel((context, options) =>
// {
//     options.ConfigureHttpsDefaults(adapterOptions =>
//     {
//         adapterOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
//     });
// });

var env = builder.Environment.EnvironmentName;
builder.Configuration.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

// Добавление OpenTelemetry для трассировки
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation() // Инструментация для ASP.NET Core
            .AddHttpClientInstrumentation() // Инструментация для HttpClient
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyDotNetApp"))
            .AddJaegerExporter(options =>
            {
                options.AgentHost = "jaeger"; // Хост Jaeger (из Docker Compose)
                options.AgentPort = 6831; // Порт Jaeger
            })
            .AddConsoleExporter(); // Вывод трассировки в консоль (для отладки)
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation() // Метрики для ASP.NET Core
            .AddHttpClientInstrumentation() // Метрики для HttpClient
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyDotNetApp"))
            .AddPrometheusExporter(); // Экспорт метрик в Prometheus
    });

//builder.WebHost.UseKestrel(options => { options.UseSystemd(); });

// Добавление Prometheus для метрик
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
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

// Добавление эндпоинта для Prometheus
app.MapPrometheusScrapingEndpoint();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}