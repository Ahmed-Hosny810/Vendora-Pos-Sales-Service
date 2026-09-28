using FluentValidation;

namespace Pos.SalesService.Application.Features.Customers.Queries.GetCustomersQuery;

public class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
    {
        RuleFor(x => x.Parameter).NotNull();
        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.OrderKey).IsInEnum();
            RuleFor(x => x.Parameter.Filter!.Search).MaximumLength(200)
                .When(x => x.Parameter.Filter != null);
        });
    }
}
