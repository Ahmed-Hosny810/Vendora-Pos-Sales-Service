using FluentValidation;

namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetReceiptQuery;

public class GetSaleReturnReceiptQueryValidator : AbstractValidator<GetSaleReturnReceiptQuery>
{
    public GetSaleReturnReceiptQueryValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
    }
}

