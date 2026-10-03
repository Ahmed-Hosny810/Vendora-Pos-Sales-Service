using FluentValidation;

namespace Pos.SalesService.Application.Features.Sales.Commands.CreateDraftCommand
{
    public class CreateSaleDraftCommandValidator
        : AbstractValidator<CreateSaleDraftCommand>
    {
        public CreateSaleDraftCommandValidator()
        {
            RuleFor(x => x.IdempotencyKey)
                .NotEmpty();

            RuleFor(x => x.Items)
                .NotEmpty();

            RuleForEach(x => x.Items)
                .NotNull();

            When(x => x.Items != null && x.Items.All(i => i != null), () =>
            {

                RuleFor(x => x.Items)
                    .Must(items => items
                        .Select(i => (i.ProductId, i.ProductVariantId))
                        .Distinct()
                        .Count() == items.Count)
                    .WithMessage(
                        "Each product/variant can appear only once. " +
                        "Increase its quantity instead of adding a duplicate.");

                RuleForEach(x => x.Items).ChildRules(item =>
                {
                    item.RuleFor(x => x.ProductId)
                        .NotEmpty();

                    item.RuleFor(x => x.ProductVariantId)
                        .Must(id => !id.HasValue || id.Value != Guid.Empty)
                        .WithMessage("Variant ID must be valid when supplied.");

                    item.RuleFor(x => x.Quantity)
                        .GreaterThan(0);
                });
            });
        }
    }
}
