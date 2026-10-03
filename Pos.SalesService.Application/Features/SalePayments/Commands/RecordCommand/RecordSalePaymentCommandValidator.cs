using FluentValidation;
namespace Pos.SalesService.Application.Features.SalePayments.Commands.RecordCommand;

public class RecordSalePaymentCommandValidator : AbstractValidator<RecordSalePaymentCommand>
{
    public RecordSalePaymentCommandValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.RowVersion).Must(value => value != null && value.Length == 8)
            .WithMessage("The sale row version is required.");
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(9999999999999999.99m)
            .Must(value => decimal.Round(value, 2) == value).WithMessage("Amount must have at most two decimal places.");
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
    }
}

