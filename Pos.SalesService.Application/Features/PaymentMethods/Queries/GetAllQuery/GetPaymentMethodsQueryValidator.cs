using FluentValidation;
namespace Pos.SalesService.Application.Features.PaymentMethods.Queries.GetAllQuery;
public class GetPaymentMethodsQueryValidator : AbstractValidator<GetPaymentMethodsQuery>
{
    public GetPaymentMethodsQueryValidator()
    {
        RuleFor(x => x.Parameter).NotNull();
        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.OrderKey).IsInEnum();
            RuleFor(x => x.Parameter.Filter!.Search).MaximumLength(80)
                .When(x => x.Parameter.Filter != null);
        });
    }
}
