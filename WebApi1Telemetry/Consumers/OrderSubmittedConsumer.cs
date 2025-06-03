using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WebApi1Telemetry.HttpClients;
using WebApi1Telemetry.Models;

namespace WebApi1Telemetry.Consumers;

public class OrderSubmittedConsumer(
    ILogger<OrderSubmittedConsumer> logger,
    ApiDbContext dbContext,
    IWebApi2TelClient api2TelClient,
    IDistributedCache cache)
    : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var specHeader = context.Headers.Get<string>("specHeader");

        var message = context.Message;

        logger.LogInformation("Подтверждение заказа {OrderId}", message.OrderId);

        try
        {
            if (!await dbContext.Orders.AnyAsync(o => o.Id == message.OrderId))
            {
                logger.LogWarning("Order {OrderId} not found in database", message.OrderId);
                
                await context.Defer(TimeSpan.FromSeconds(10));
                return;
            }
            
            var order = await dbContext.Orders
                .SingleOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);

            var orderMarker = await api2TelClient.GetOrderMarker(order!.Id, context.CancellationToken);
            var serializedMarker = JsonSerializer.Serialize(new { orderMarker, order.Id, DateTime.UtcNow, specHeader });

            order.SubmittedAt = DateTime.UtcNow;
            order.Submitter = message.Submitter;
            order.OrderMarker = serializedMarker;
            order.FeedbackEmail = message.FeedbackEmail;
            
            dbContext.Orders.Update(order);
    
            // Сохраняем в БД
            await dbContext.SaveChangesAsync(context.CancellationToken);
            
            var cacheKey = $"OrderItemsCacheKey_{order.Id}";
            await cache.RemoveAsync(cacheKey);
            await context.Publish(new RecalcOrderCacheEvent(order.Id), context.CancellationToken);
            
            logger.LogInformation("Заказ {OrderId} успешно обновлен в базе данных", message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при сохранении заказа {OrderId} в базу данных", message.OrderId);

            // Вы можете выбросить исключение для повторной обработки сообщения
            // или обработать ошибку иным образом
            //throw;
        }
    }
}