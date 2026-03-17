namespace StockFlow.Domain.Entities;

public class IdempotencyRecord
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string Key { get; set; } = default!;
  public string RequestType { get; set; } = default!;
  public string RequestHash { get; set; } = default!;
  public string Status { get; set; } = default!;
  public Guid? OrderId { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? CompletedAt { get; set; }
}