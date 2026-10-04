using FluentValidation;
using Pos.SalesService.Application.Features.SalesReturns.Validators;

namespace Pos.SalesService.Application.Features.SalesReturns.Commands.CancelReturnDraftCommand;

public class CancelSaleReturnDraftCommandValidator : AbstractValidator<CancelSaleReturnDraftCommand>
{
    public CancelSaleReturnDraftCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
        RuleFor(x => x.RowVersion).NotNull().Must(x => x != null && x.Length == 8)
            .WithMessage("A valid row version is required.");
    }
}

