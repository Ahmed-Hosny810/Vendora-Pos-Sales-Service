using FluentValidation;
namespace Pos.SalesService.Application.Features.PaymentMethods.Queries.GetByIdQuery;
public class GetPaymentMethodByIdQueryValidator : AbstractValidator<GetPaymentMethodByIdQuery>
{
    public GetPaymentMethodByIdQueryValidator()
    {
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}
