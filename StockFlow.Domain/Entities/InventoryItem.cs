namespace StockFlow.Domain.Entities;

public class InventoryItem
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public Guid ProductId { get; set; }
  public int OnHand { get; set; }
  public int Reserved { get; set; }
  public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
  public Product Product { get; set; } = default!;
  public byte[] RowVersion { get; set; } = default!;
}