using FluentValidation;
using PhoneNumbers;

namespace Pos.SalesService.Application.Features.Customers.Commands.CreateCommand
{
    public class CreateCustomerCommandValidator
        : AbstractValidator<CreateCustomerCommand>
    {
        public CreateCustomerCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Phone)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Phone number can't be empty");

            RuleFor(x => x.AddressLine)
                .MaximumLength(500);

            RuleFor(x => x.Area)
                .MaximumLength(100);

            RuleFor(x => x.City)
                .MaximumLength(100);

            RuleFor(x => x.Notes)
                .MaximumLength(500);
        }

    }
}
