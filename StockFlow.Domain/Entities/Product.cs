namespace StockFlow.Domain.Entities;

public class Product
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public string Sku { get; set; } = default!;
  public string Name { get; set; } = default!;

  public bool IsActive { get; set; } = true;
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}