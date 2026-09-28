using Pos.SalesService.Application.Features.Customers.Queries.GetCustomersQuery;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.QueryExtensions;

public static class CustomerQueryExtensions
{
    public static IQueryable<Customer> ApplyFilter(this IQueryable<Customer> query, Guid tenantId, CustomerFilter? filter)
    {
        query = query.Where(x => x.TenantId == tenantId);
        if (filter == null) return query;
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x => x.FullName.Contains(search) || x.Phone.Contains(search));
        }
        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        return query;
    }

    public static IOrderedQueryable<Customer> ApplyOrdering(this IQueryable<Customer> query,
        CustomerOrderKey orderKey, bool descending)
    {
        var ordered = orderKey switch
        {
            CustomerOrderKey.FullName => descending ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName),
            CustomerOrderKey.Phone => descending ? query.OrderByDescending(x => x.Phone) : query.OrderBy(x => x.Phone),
            CustomerOrderKey.UpdatedAt => descending ? query.OrderByDescending(x => x.UpdatedAt) : query.OrderBy(x => x.UpdatedAt),
            _ => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
        return ordered.ThenBy(x => x.Id);
    }
}
