using StockFlow.Application.Common;


namespace StockFlow.Application.Orders;

public interface IOrderService
{
  Task<Result<OrderDto>> CreateAsync(CreateOrderRequest req, string IdempotencyKey, CancellationToken ct);
  Task<OrderDto> GetByIdAsync(Guid id, CancellationToken ct);
  Task<IReadOnlyList<OrderDto>> GetAllAsync(string? status, CancellationToken ct);
  Task<OrderDto> CancelAsync(Guid id, CancellationToken ct);
  Task<OrderDto> ConfirmAsync(Guid id, CancellationToken ct);
  Task<OrderDto> ShipAsync(Guid id, CancellationToken ct);
  Task<OrderDto> CompleteAsync(Guid id, CancellationToken ct);
}