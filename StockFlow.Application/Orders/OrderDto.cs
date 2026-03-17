using StockFlow.Domain.Entities;

namespace StockFlow.Application.Orders;

public record OrderDto(
  Guid Id,
  string OrderNumber,
  string Status,
  string CustomerName,
  decimal TotalAmount,
  DateTimeOffset CreatedAt,
  List<OrderLineDto> Lines
);

public record OrderLineDto(
  Guid ProductId, 
  int Quantity,
  decimal UnitPrice
);