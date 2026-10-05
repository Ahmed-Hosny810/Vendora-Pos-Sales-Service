using FluentValidation;

namespace Pos.SalesService.Application.Features.RefundPayments.Commands.RecordRefundCommand
{
    public class RecordRefundPaymentCommandValidator : AbstractValidator<RecordRefundPaymentCommand>
    {
        public RecordRefundPaymentCommandValidator()
        {
            RuleFor(x => x.ReturnId).NotEmpty();
            RuleFor(x => x.PaymentMethodId).NotEmpty();
            RuleFor(x => x.IdempotencyKey).NotEmpty();
            RuleFor(x => x.ReferenceNumber)
                .MaximumLength(100);
        }
    }
}
