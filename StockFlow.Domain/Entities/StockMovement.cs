namespace StockFlow.Domain.Entities;

public class StockMovement
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public Guid ProductId { get; set; } 
  public int QtyDelta { get; set; }
  public string? Type { get; set; } = default!;
  public string? RefId { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  public Product Product { get; set; } = default!;
}