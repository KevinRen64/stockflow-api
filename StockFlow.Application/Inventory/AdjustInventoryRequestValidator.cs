using FluentValidation;

namespace StockFlow.Application.Inventory;

public class AdjustInventoryRequestValidator : AbstractValidator<AdjustInventoryRequest>
{
  public AdjustInventoryRequestValidator()
  {
    RuleFor( x => x.ProductId)
        .NotEmpty();

    RuleFor( x => x.QuantityDelta)
        .NotEqual(0);
  }
}