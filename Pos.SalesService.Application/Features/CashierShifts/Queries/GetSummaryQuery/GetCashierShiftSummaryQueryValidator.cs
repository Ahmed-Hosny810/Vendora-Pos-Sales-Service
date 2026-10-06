using FluentValidation;

namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetSummaryQuery
{
    public class GetCashierShiftSummaryQueryValidator : AbstractValidator<GetCashierShiftSummaryQuery>
    {
        public GetCashierShiftSummaryQueryValidator()
        {
            RuleFor(x => x.ShiftId)
                .NotEmpty();
        }
    }
}
