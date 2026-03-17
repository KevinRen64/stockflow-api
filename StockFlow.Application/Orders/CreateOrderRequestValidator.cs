using System.Data;
using FluentValidation;

namespace StockFlow.Application.Orders;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
  public CreateOrderRequestValidator()
  {
    RuleFor(x => x.ProductId)
      .NotEmpty();
    
    RuleFor(x => x.Quantity)
      .GreaterThan(0);
    
    RuleFor(x => x.CustomerName)
      .NotEmpty()
      .MaximumLength(200);
  }
}