using FluentValidation;

namespace Pos.SalesService.Application.Features.CashierShifts.Commands.OpenShiftCommand
{
    public class OpenCashierShiftCommandValidator
        : AbstractValidator<OpenCashierShiftCommand>
    {
        public OpenCashierShiftCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty();

            RuleFor(x => x.TerminalId)
                .NotEmpty();

            RuleFor(x => x.OpeningCash)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Opening cash cannot be negative.")
                .PrecisionScale(18, 2, true)
                .WithMessage(
                    "Opening cash must fit decimal(18,2).");
        }
    }
}
