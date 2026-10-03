using FluentValidation;

namespace Pos.SalesService.Application.Features.Sales.Queries.GetByIdQuery
{
    public class GetSaleByIdQueryValidator : AbstractValidator<GetSaleByIdQuery>
    {
        public GetSaleByIdQueryValidator()
        {
            RuleFor(x => x.SaleId).NotEmpty();
        }
    }
}