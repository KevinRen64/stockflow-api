using StockFlow.Application.Common;

namespace StockFlow.Application.Inventory;

public interface IInventoryService
{
  Task<Result<InventoryDto>> AdjustAsync(AdjustInventoryRequest request, CancellationToken ct);
  Task<Result<InventoryDto>> GetByProductIdAsync(Guid productId, CancellationToken ct);
}