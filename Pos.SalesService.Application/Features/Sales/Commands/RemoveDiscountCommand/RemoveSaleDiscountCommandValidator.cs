using FluentValidation;

namespace Pos.SalesService.Application.Features.Sales.Commands.RemoveDiscountCommand
{
    public class RemoveSaleDiscountCommandValidator
        : AbstractValidator<RemoveSaleDiscountCommand>
    {
        public RemoveSaleDiscountCommandValidator()
        {
            RuleFor(x => x.SaleId)
                .NotEmpty();

            RuleFor(x => x.DiscountId)
                .NotEmpty();

            RuleFor(x => x.RowVersion)
                .Must(value => value != null && value.Length == 8)
                .WithMessage("A valid row version is required.");
        }
    }
}
