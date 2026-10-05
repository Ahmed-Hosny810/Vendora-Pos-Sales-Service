using Pos.SalesService.Application.Features.SalesReturns.Queries.GetAllQuery;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.QueryExtensions;

public static class SaleReturnQueryExtensions
{
    public static IQueryable<SaleReturn> ApplyFilter(this IQueryable<SaleReturn> query,
        Guid tenantId, SaleReturnFilter? filter)
    {
        query = query.Where(x => x.TenantId == tenantId);
        if (filter == null) return query;
        if (filter.OriginalSaleId.HasValue)
            query = query.Where(x => x.OriginalSaleId == filter.OriginalSaleId.Value);
        if (filter.BranchId.HasValue)
            query = query.Where(x => x.BranchId == filter.BranchId.Value);
        if (filter.ProcessedByUserId.HasValue)
            query = query.Where(x => x.ProcessedByUserId == filter.ProcessedByUserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.ReturnNumber))
            query = query.Where(x => x.ReturnNumber == filter.ReturnNumber.Trim());
        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.CompletedAt >= filter.FromUtc.Value);
        if (filter.ToUtcExclusive.HasValue)
            query = query.Where(x => x.CompletedAt < filter.ToUtcExclusive.Value);
        return query;
    }

    public static IOrderedQueryable<SaleReturn> ApplyOrdering(this IQueryable<SaleReturn> query,
        SaleReturnOrderKey orderKey, bool descending)
    {
        var ordered = orderKey switch
        {
            SaleReturnOrderKey.RefundAmount => descending
                ? query.OrderByDescending(x => x.RefundAmount) : query.OrderBy(x => x.RefundAmount),
            SaleReturnOrderKey.ReturnNumber => descending
                ? query.OrderByDescending(x => x.ReturnNumber) : query.OrderBy(x => x.ReturnNumber),
            _ => descending ? query.OrderByDescending(x => x.CompletedAt) : query.OrderBy(x => x.CompletedAt)
        };
        return ordered.ThenBy(x => x.Id);
    }
}

