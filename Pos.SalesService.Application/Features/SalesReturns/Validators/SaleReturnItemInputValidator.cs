using FluentValidation;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.SalesReturns.Validators;

public class SaleReturnItemInputValidator : AbstractValidator<SaleReturnItemInput>
{
    public SaleReturnItemInputValidator()
    {
        RuleFor(x => x.OriginalSaleItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 3, true);
        RuleFor(x => x.StockCondition).Must(x =>
            x == StockCondition.Sellable || x == StockCondition.Damaged || x == StockCondition.Expired)
            .WithMessage("Select Sellable, Damaged or Expired.");
        RuleFor(x => x.Restock).Equal(false)
            .When(x => x.StockCondition != StockCondition.Sellable)
            .WithMessage("Damaged or expired items cannot be restocked as sellable stock.");
    }
}
