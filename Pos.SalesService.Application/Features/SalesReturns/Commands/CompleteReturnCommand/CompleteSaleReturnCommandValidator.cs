using FluentValidation;
using Pos.SalesService.Application.Features.SalesReturns.Validators;

namespace Pos.SalesService.Application.Features.SalesReturns.Commands.CompleteReturnCommand;

public class CompleteSaleReturnCommandValidator : AbstractValidator<CompleteSaleReturnCommand>
{
    public CompleteSaleReturnCommandValidator()
    {
        RuleFor(x => x.OriginalSaleId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Items).NotEmpty()
            .Must(x => x == null || x.Count <= 500)
            .WithMessage("A return can contain at most 500 items.");
        RuleFor(x => x.Items).Must(items => items == null ||
            items.Where(x => x != null).Select(x => x.OriginalSaleItemId).Distinct().Count() == items.Count)
            .WithMessage("Each original sale item can appear only once in a return request.");
        RuleForEach(x => x.Items).NotNull().SetValidator(new SaleReturnItemInputValidator());
    }
}
