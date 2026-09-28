using FluentValidation;

namespace Pos.SalesService.Application.Features.Customers.Queries.GetByIdQuery;

public class GetCustomerByIdQueryValidator : AbstractValidator<GetCustomerByIdQuery>
{
    public GetCustomerByIdQueryValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
