using FluentValidation;

namespace Pos.SalesService.Application.Features.Customers.Commands.UpdateCommand;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.AddressLine).MaximumLength(500);
        RuleFor(x => x.Area).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.RowVersion).Cascade(CascadeMode.Stop).NotEmpty()
            .Must(x => x.Length == 8).WithMessage("A valid customer row version is required.");
    }
}
