using FluentValidation;
using Pos.SalesService.Domain.Constants;
namespace Pos.SalesService.Application.Features.SalePayments.Queries.GetAllQuery;

public class GetSalePaymentsQueryValidator : AbstractValidator<GetSalePaymentsQuery>
{
    public GetSalePaymentsQueryValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.Parameter).NotNull();
        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.OrderKey).IsInEnum();
            When(x => x.Parameter.Filter != null, () =>
            {
                RuleFor(x => x.Parameter.Filter!.Status).Must(s => string.IsNullOrWhiteSpace(s) ||
                    s == PaymentStatus.Pending || s == PaymentStatus.Completed ||
                    s == PaymentStatus.Failed || s == PaymentStatus.Cancelled).WithMessage("Invalid payment status.");
                RuleFor(x => x.Parameter.Filter!.PaymentMethodId)
                    .Must(id => !id.HasValue || id != Guid.Empty).WithMessage("Invalid payment method ID.");
            });
        });
    }
}

