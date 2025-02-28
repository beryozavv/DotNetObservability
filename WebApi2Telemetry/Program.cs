var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var env = builder.Environment.EnvironmentName;
builder.Configuration.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

// Добавление OpenTelemetry для трассировки
// builder.Services.AddOpenTelemetry()
//     .WithTracing(tracing =>
//     {
//         tracing
//             .AddAspNetCoreInstrumentation() // Инструментация для ASP.NET Core
//             .AddHttpClientInstrumentation() // Инструментация для HttpClient
//             .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyDotNetApp"))
//             .AddJaegerExporter(options =>
//             {
//                 options.AgentHost = "jaeger"; // Хост Jaeger (из Docker Compose)
//                 options.AgentPort = 6831; // Порт Jaeger
//             })
//             .AddConsoleExporter(); // Вывод трассировки в консоль (для отладки)
//     })
//     .WithMetrics(metrics =>
//     {
//         metrics
//             .AddAspNetCoreInstrumentation() // Метрики для ASP.NET Core
//             .AddHttpClientInstrumentation() // Метрики для HttpClient
//             .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyDotNetApp"))
//             .AddPrometheusExporter(); // Экспорт метрик в Prometheus
//     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}