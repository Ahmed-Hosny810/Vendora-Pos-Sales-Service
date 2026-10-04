using FluentValidation;
namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetReturnableItemsQuery;

public class GetReturnableSaleItemsQueryValidator : AbstractValidator<GetReturnableSaleItemsQuery>
{
    public GetReturnableSaleItemsQueryValidator() { RuleFor(x => x.SaleId).NotEmpty(); }
}

