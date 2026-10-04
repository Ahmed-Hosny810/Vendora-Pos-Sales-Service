using FluentValidation;
using Pos.SalesService.Domain.Constants;
namespace Pos.SalesService.Application.Features.Sales.Queries.GetStatusHistoryQuery;
public class GetSaleStatusHistoryQueryValidator : AbstractValidator<GetSaleStatusHistoryQuery>
{
    public GetSaleStatusHistoryQueryValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
        RuleFor(x => x.Parameter).NotNull();
        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.OrderKey).IsInEnum();
            When(x => x.Parameter.Filter != null, () =>
            {
                RuleFor(x => x.Parameter.Filter!).Must(f =>
                    !f.FromUtc.HasValue || !f.ToUtcExclusive.HasValue || f.FromUtc < f.ToUtcExclusive)
                    .WithMessage("FromUtc must be earlier than ToUtcExclusive.");
                RuleFor(x => x.Parameter.Filter!.FromUtc).Must(d => !d.HasValue || d.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("FromUtc must be a UTC timestamp ending in Z.");
                RuleFor(x => x.Parameter.Filter!.ToUtcExclusive).Must(d => !d.HasValue || d.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("ToUtcExclusive must be a UTC timestamp ending in Z.");
                RuleFor(x => x.Parameter.Filter!.NewStatus).Must(IsKnownStatus)
                    .WithMessage("Invalid sale status.");
                RuleFor(x => x.Parameter.Filter!.ChangedByUserId).Must(id => !id.HasValue || id != Guid.Empty);
            });
        });
    }
    private static bool IsKnownStatus(string? status) => string.IsNullOrWhiteSpace(status) ||
        status == SaleStatus.Draft || status == SaleStatus.CheckoutPending ||
        status == SaleStatus.PendingPayment || status == SaleStatus.Completing ||
        status == SaleStatus.Completed || status == SaleStatus.Cancelled ||
        status == SaleStatus.PartiallyReturned || status == SaleStatus.Returned;
}

