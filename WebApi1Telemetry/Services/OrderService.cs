using MassTransit;
using WebApi1Telemetry.Models;

namespace WebApi1Telemetry.Services;

public class OrderService : IOrderService
{
    private readonly IBus _bus;
    private readonly ApiDbContext _context;

    public OrderService(IBus bus, ApiDbContext context)
    {
        _bus = bus;
        _context = context;
    }

    public async Task<Guid> AddNewOrder(Order message, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = message.Id,
            CustomerName = message.CustomerName,
            Email = message.Email,
            TotalAmount = message.TotalAmount,
            CreatedAt = message.CreatedAt,
            Items = message.Items.Select(item => new OrderItem
            {
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        // Добавляем в контекст
        _context.Orders.Add(order);

        // Сохраняем в БД
        await _context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    public async Task<Guid> SubmitOrder(Guid id, CancellationToken cancellationToken)
    {
        // Создаем сообщение на основе DTO
        var message = new OrderSubmitted
        {
            OrderId = id,
            FeedbackEmail = "feedback@test.com",
            Submitter = "testEmployer"
        };

        // Отправляем сообщение
        await _bus.Publish(message, cancellationToken);

        return id;
    }
}