using FluentValidation;
namespace StockFlow.Application.Demo;

public class DemoRequestValidator : AbstractValidator<DemoRequest>
{
      public DemoRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3);
    }
}