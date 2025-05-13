using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebApi2Telemetry;
using WebApi2Telemetry.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var env = builder.Environment.EnvironmentName;
builder.Configuration.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

var telemetryOptions = builder.Configuration.GetSection(nameof(TelemetrySettings)).Get<TelemetrySettings>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation() // Инструментация для ASP.NET Core
            .AddHttpClientInstrumentation() // Инструментация для HttpClient
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApi2Telemetry"))
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
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("WebApi2Telemetry"))
            .AddPrometheusExporter(); // Экспорт метрик в Prometheus
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
/*if (app.Environment.IsDevelopment())
{*/
    app.UseSwagger();
    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

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

app.MapGet("/getordermarker/{id}", (Guid id) =>
    {
        var marker = $"My_test_marker_{DateTime.Now.ToString("O")}_{id}";
        return marker;
    })
    .WithName("GetOrderMarker")
    .WithOpenApi();

app.Run();

namespace WebApi2Telemetry
{
    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}