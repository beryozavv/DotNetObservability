using MassTransit;
using Microsoft.EntityFrameworkCore;
using WebApi1Telemetry.HttpClients;
using WebApi1Telemetry.Models;

namespace WebApi1Telemetry.Consumers;

public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    private readonly ILogger<OrderSubmittedConsumer> _logger;
    private readonly ApiDbContext _dbContext;
    private readonly IWebApi2TelClient _api2TelClient;

    public OrderSubmittedConsumer(
        ILogger<OrderSubmittedConsumer> logger,
        ApiDbContext dbContext, IWebApi2TelClient api2TelClient)
    {
        _logger = logger;
        _dbContext = dbContext;
        _api2TelClient = api2TelClient;
    }

    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var message = context.Message;

        _logger.LogInformation("Подтверждение заказа {OrderId}", message.OrderId);

        try
        {
            var order = await _dbContext.Orders.Include(o => o.Items)
                .SingleOrDefaultAsync(o => o.Id == message.OrderId, context.CancellationToken);

            var orderMarker = await _api2TelClient.GetOrderMarker(order!.Id);

            order.SubmittedAt = DateTime.UtcNow;
            order.Submitter = message.Submitter;
            order.OrderMarker = orderMarker;
            order.FeedbackEmail = message.FeedbackEmail;
    
            // Сохраняем в БД
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Заказ {OrderId} успешно обновлен в базе данных", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении заказа {OrderId} в базу данных", message.OrderId);

            // Вы можете выбросить исключение для повторной обработки сообщения
            // или обработать ошибку иным образом
            //throw;
        }
    }
}