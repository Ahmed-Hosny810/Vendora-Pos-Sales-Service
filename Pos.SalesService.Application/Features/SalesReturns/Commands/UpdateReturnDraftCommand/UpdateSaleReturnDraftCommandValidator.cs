using FluentValidation;
using Pos.SalesService.Application.Features.SalesReturns.Validators;

namespace Pos.SalesService.Application.Features.SalesReturns.Commands.UpdateReturnDraftCommand;

public class UpdateSaleReturnDraftCommandValidator : AbstractValidator<UpdateSaleReturnDraftCommand>
{
    public UpdateSaleReturnDraftCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
        RuleFor(x => x.RowVersion).NotNull().Must(x => x != null && x.Length == 8)
            .WithMessage("A valid row version is required.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Items).NotEmpty().Must(x => x == null || x.Count <= 500)
            .WithMessage("A return can contain at most 500 items.");
        RuleFor(x => x.Items).Must(items => items == null ||
            items.Where(i => i != null).Select(i => (i.OriginalSaleItemId, i.StockCondition, i.Restock)).Distinct().Count() == items.Count)
            .WithMessage("Combine entries with the same original item, condition and restock choice.");
        RuleForEach(x => x.Items).NotNull().SetValidator(new SaleReturnItemInputValidator());
    }
}

