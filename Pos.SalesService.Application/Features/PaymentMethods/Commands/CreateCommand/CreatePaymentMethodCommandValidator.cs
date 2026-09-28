using FluentValidation;
namespace Pos.SalesService.Application.Features.PaymentMethods.Commands.CreateCommand;
public class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);

    }
}
