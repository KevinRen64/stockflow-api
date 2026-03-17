namespace StockFlow.Domain.Entities;

public class Order
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string OrderNumber { get; set; } = default!;
  public string Status { get; set; } = "Pending";
  public string CustomerName { get; set; } = default!;
  public decimal TotalAmount { get; set; }
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  public List<OrderLine> Lines { get; set; } = new();
}