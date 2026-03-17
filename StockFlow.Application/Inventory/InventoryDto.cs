namespace StockFlow.Application.Inventory;

public record InventoryDto(
    Guid Id,
    Guid ProductId,
    int OnHand,
    int Reserved,
    int Available,
    DateTimeOffset UpdatedAt
);