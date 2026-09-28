using FluentValidation;
using Pos.SalesService.Domain.Constants;
namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetAllQuery;
public class GetCashierShiftsQueryValidator : AbstractValidator<GetCashierShiftsQuery>
{
    public GetCashierShiftsQueryValidator()
    {
        RuleFor(x => x.Parameter).NotNull();
        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.OrderKey).IsInEnum();
            When(x => x.Parameter.Filter != null, () =>
            {
                RuleFor(x => x.Parameter.Filter!.BranchId).Must(x => !x.HasValue || x.Value != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.TerminalId).Must(x => !x.HasValue || x.Value != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.CashierUserId).Must(x => !x.HasValue || x.Value != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.Status).Must(x => string.IsNullOrWhiteSpace(x) ||
                    x == CashierShiftStatus.Open || x == CashierShiftStatus.Closed)
                    .WithMessage("Status must be Open or Closed.");
                RuleFor(x => x.Parameter.Filter).Must(x => !x!.FromUtc.HasValue ||
                    !x.ToUtcExclusive.HasValue || x.FromUtc.Value < x.ToUtcExclusive.Value)
                    .WithMessage("FromUtc must be earlier than ToUtcExclusive.");
            });
        });
    }
}
