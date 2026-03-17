namespace StockFlow.Application.Orders;

public record CreateOrderRequest(
  Guid ProductId,
  int Quantity,
  string CustomerName
);