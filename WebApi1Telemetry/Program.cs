using Microsoft.EntityFrameworkCore;
using WebApi1Telemetry;
using WebApi1Telemetry.Data;
using WebApi1Telemetry.Extensions;
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

builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRefitHttpClients();

// Добавляем Redis в DI
builder.Services.AddRedisCache(builder.Configuration);

// Добавляем MassTransit в DI
builder.Services.ConfigureMassTransit(builder.Configuration);

// Добавление OpenTelemetry для трассировки
builder.Services.AddMetricsAndTracing(builder.Configuration);

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

// Команда для засеивания данных
if (true)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApiDbContext>();

    await SeedData.Initialize(context);
    Console.WriteLine("Data seeded successfully");
}

app.Run();