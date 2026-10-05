using FluentValidation;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.RefundPayments.Queries.GetAllQuery;

public class GetRefundPaymentsQueryValidator : AbstractValidator<GetRefundPaymentsQuery>
{
    public GetRefundPaymentsQueryValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
        RuleFor(x => x.Parameter).NotNull();
        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.OrderKey).IsInEnum();
            When(x => x.Parameter.Filter != null, () =>
            {
                RuleFor(x => x.Parameter.Filter!.PaymentMethodId).Must(x => x != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.ShiftId).Must(x => x != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.PaidByUserId).Must(x => x != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.Status).Must(x => string.IsNullOrWhiteSpace(x) ||
                    x == PaymentStatus.Completed || x == PaymentStatus.Pending ||
                    x == PaymentStatus.Failed || x == PaymentStatus.Cancelled)
                    .WithMessage("Invalid payment status.");
                RuleFor(x => x.Parameter.Filter!.FromUtc)
                    .Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("Use a UTC timestamp ending in Z.");
                RuleFor(x => x.Parameter.Filter!.ToUtcExclusive)
                    .Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("Use a UTC timestamp ending in Z.");
                RuleFor(x => x.Parameter.Filter).Must(x => !x!.FromUtc.HasValue ||
                    !x.ToUtcExclusive.HasValue || x.FromUtc < x.ToUtcExclusive)
                    .WithMessage("The start date must precede the exclusive end date.");
            });
        });
    }
}

