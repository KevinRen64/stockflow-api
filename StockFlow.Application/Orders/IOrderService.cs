using StockFlow.Application.Common;


namespace StockFlow.Application.Orders;

public interface IOrderService
{
  Task<Result<OrderDto>> CreateAsync(CreateOrderRequest req, string IdempotencyKey, CancellationToken ct);
  Task<Result<OrderDto>> GetByIdAsync(Guid id, CancellationToken ct);
  Task<IReadOnlyList<OrderDto>> GetAllAsync(string? status, CancellationToken ct);
  Task<Result<OrderDto>> CancelAsync(Guid id, CancellationToken ct);
  Task<Result<OrderDto>> ConfirmAsync(Guid id, CancellationToken ct);
  Task<Result<OrderDto>> ShipAsync(Guid id, CancellationToken ct);
  Task<Result<OrderDto>> CompleteAsync(Guid id, CancellationToken ct);
}