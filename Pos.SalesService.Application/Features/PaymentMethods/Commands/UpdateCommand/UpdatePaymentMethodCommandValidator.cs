using FluentValidation;
namespace Pos.SalesService.Application.Features.PaymentMethods.Commands.UpdateCommand;
public class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    public UpdatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.RowVersion).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(x => x.Length == 8).WithMessage("A valid payment method row version is required.");
    }
}
