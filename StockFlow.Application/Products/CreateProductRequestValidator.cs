using FluentValidation;


namespace StockFlow.Application.Products;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
  public CreateProductRequestValidator()
  {
    RuleFor(x => x.Sku)
      .NotEmpty()
      .MaximumLength(50);
    
    RuleFor(x => x.Name)
      .NotEmpty()
      .MaximumLength(200);
  }  
}