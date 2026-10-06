using FluentValidation;

namespace Pos.SalesService.Application.Features.CashierShifts.Commands.CloseShiftCommand
{
    public class CloseCashierShiftCommandValidator : AbstractValidator<CloseCashierShiftCommand>
    {
        public CloseCashierShiftCommandValidator()
        {
            RuleFor(x => x.ClosingCash)
                .GreaterThanOrEqualTo(0)
                .PrecisionScale(18, 2, true);

            RuleFor(x => x.RowVersion)
                .NotEmpty();
        }
    }
}