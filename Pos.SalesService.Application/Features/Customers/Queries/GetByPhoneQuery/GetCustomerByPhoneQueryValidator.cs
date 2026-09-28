using FluentValidation;

namespace Pos.SalesService.Application.Features.Customers.Queries.GetByPhoneQuery;

public class GetCustomerByPhoneQueryValidator : AbstractValidator<GetCustomerByPhoneQuery>
{
    public GetCustomerByPhoneQueryValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
    }
}
