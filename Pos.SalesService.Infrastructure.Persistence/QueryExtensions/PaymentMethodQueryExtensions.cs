using Pos.SalesService.Application.Features.PaymentMethods.Queries.GetAllQuery;
using Pos.SalesService.Domain.Models;
namespace Pos.SalesService.Infrastructure.Persistence.QueryExtensions;
public static class PaymentMethodQueryExtensions
{
    public static IQueryable<PaymentMethod> ApplyFilter(this IQueryable<PaymentMethod> query,
        Guid tenantId, PaymentMethodFilter? filter)
    {
        query = query.Where(x => x.TenantId == tenantId);
        if (filter == null) return query;
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.Code.Contains(search));
        }
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        if (filter.IsCash.HasValue) query = query.Where(x => x.IsCash == filter.IsCash.Value);
        return query;
    }
    public static IOrderedQueryable<PaymentMethod> ApplyOrdering(this IQueryable<PaymentMethod> query,
        PaymentMethodOrderKey key, bool descending)
    {
        var ordered = key switch
        {
            PaymentMethodOrderKey.Name => descending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            PaymentMethodOrderKey.Code => descending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            PaymentMethodOrderKey.CreatedAt => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => descending ? query.OrderByDescending(x => x.SortOrder) : query.OrderBy(x => x.SortOrder)
        };
        return ordered.ThenBy(x => x.Id);
    }
}
