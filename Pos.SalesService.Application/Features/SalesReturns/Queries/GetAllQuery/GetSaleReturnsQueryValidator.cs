using FluentValidation;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetAllQuery;

public class GetSaleReturnsQueryValidator : AbstractValidator<GetSaleReturnsQuery>
{
    public GetSaleReturnsQueryValidator()
    {
        RuleFor(x => x.Parameter).NotNull();
        When(x => x.Parameter != null, () =>
        {
            RuleFor(x => x.Parameter.OrderKey).IsInEnum();
            When(x => x.Parameter.Filter != null, () =>
            {
                RuleFor(x => x.Parameter.Filter!.OriginalSaleId).Must(x => x != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.BranchId).Must(x => x != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.ProcessedByUserId).Must(x => x != Guid.Empty);
                RuleFor(x => x.Parameter.Filter!.ReturnNumber).MaximumLength(80);
                RuleFor(x => x.Parameter.Filter!.FromUtc).Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("Use a UTC timestamp ending in Z.");
                RuleFor(x => x.Parameter.Filter!.ToUtcExclusive).Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc)
                    .WithMessage("Use a UTC timestamp ending in Z.");
                RuleFor(x => x.Parameter.Filter).Must(x => !x!.FromUtc.HasValue ||
                    !x.ToUtcExclusive.HasValue || x.FromUtc < x.ToUtcExclusive)
                    .WithMessage("The start date must precede the exclusive end date.");
            });
        });
    }
}

