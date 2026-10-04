using FluentValidation;
namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetByIdQuery;

public class GetSaleReturnByIdQueryValidator : AbstractValidator<GetSaleReturnByIdQuery>
{
    public GetSaleReturnByIdQueryValidator() { RuleFor(x => x.ReturnId).NotEmpty(); }
}

