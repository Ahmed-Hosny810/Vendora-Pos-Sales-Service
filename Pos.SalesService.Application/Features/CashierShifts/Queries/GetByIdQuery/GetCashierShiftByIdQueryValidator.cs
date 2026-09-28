using FluentValidation;
namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetByIdQuery;
public class GetCashierShiftByIdQueryValidator : AbstractValidator<GetCashierShiftByIdQuery>
{
    public GetCashierShiftByIdQueryValidator() { RuleFor(x => x.ShiftId).NotEmpty(); }
}
