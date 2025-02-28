using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WebApi1Telemetry.Extensions;
using WebApi1Telemetry.Models;
using WebApi1Telemetry.Services;

namespace WebApi1Telemetry.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ApiDbContext _context;
    private readonly IDistributedCache _cache;


    public OrderController(IOrderService orderService, ApiDbContext context, IDistributedCache cache)
    {
        _orderService = orderService;
        _context = context;
        _cache = cache;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] Order order, CancellationToken cancellationToken)
    {
        var orderId = await _orderService.AddNewOrder(order, cancellationToken);

        return Accepted(new { OrderId = orderId });
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitOrder(Guid id, CancellationToken cancellationToken)
    {
        var orderId = await _orderService.SubmitOrder(id, cancellationToken);

        return Accepted(new { OrderId = orderId });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        var cacheKey = "OrdersCacheKey";

        var orders = await _cache.GetOrCreateAsync(
            cacheKey,
            async () => await _context.Orders.ToArrayAsync(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20)
            });

        return Ok(orders);
    }

    [HttpGet("{id}/items")]
    public async Task<ActionResult<Order>> GetOrderItems(Guid id)
    {
        var cacheKey = $"OrderItemsCacheKey_{id}";

        var order = await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var fetchedOrder = await _context.Orders
                    .Include(u => u.Items)
                    .SingleOrDefaultAsync(u => u.Id == id);

                if (fetchedOrder == null)
                {
                    throw new KeyNotFoundException($"Order with ID {id} not found.");
                }

                return fetchedOrder;
            },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

        return Ok(order);
    }
}