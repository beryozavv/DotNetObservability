using WebApi1Telemetry.Models;

namespace WebApi1Telemetry.Services;

public interface IOrderService
{
    Task<Guid> AddNewOrder(Order message, CancellationToken cancellationToken);
    Task<Guid> SubmitOrder(Guid id, CancellationToken cancellationToken);
}