using FluentValidation;
namespace Pos.SalesService.Application.Features.Sales.Queries.GetReceiptQuery;
public class GetSaleReceiptQueryValidator : AbstractValidator<GetSaleReceiptQuery>
{
    public GetSaleReceiptQueryValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty();
    }
}
