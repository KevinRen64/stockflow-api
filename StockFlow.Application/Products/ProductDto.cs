namespace StockFlow.Application.Products;

public record ProductDto(
  Guid Id,
  string Sku,
  string Name,
  bool IsActive,
  DateTimeOffset CreatedAt
);
