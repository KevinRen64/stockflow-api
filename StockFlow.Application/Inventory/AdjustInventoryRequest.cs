namespace StockFlow.Application.Inventory;

public record AdjustInventoryRequest(Guid ProductId, int QuantityDelta);