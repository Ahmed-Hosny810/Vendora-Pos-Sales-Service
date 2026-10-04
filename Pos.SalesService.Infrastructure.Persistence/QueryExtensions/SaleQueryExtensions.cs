using Pos.SalesService.Application.Features.Sales.Queries.GetSalesQuery;
using Pos.SalesService.Application.Features.Sales.Queries.GetStatusHistoryQuery;
using Pos.SalesService.Domain.Models;
namespace Pos.SalesService.Infrastructure.Persistence.QueryExtensions;
public static class SaleQueryExtensions
{
    public static IQueryable<Sale> ApplyFilter(this IQueryable<Sale> query, Guid tenantId, SaleFilter? filter)
    {
        query = query.Where(s => s.TenantId == tenantId);
        if (filter == null) return query;
        if (filter.BranchId.HasValue) query = query.Where(s => s.BranchId == filter.BranchId);
        if (filter.TerminalId.HasValue) query = query.Where(s => s.TerminalId == filter.TerminalId);
        if (filter.ShiftId.HasValue) query = query.Where(s => s.ShiftId == filter.ShiftId);
        if (filter.CashierUserId.HasValue) query = query.Where(s => s.CreatedByUserId == filter.CashierUserId);
        if (filter.CustomerId.HasValue) query = query.Where(s => s.CustomerId == filter.CustomerId);
        if (filter.FromUtc.HasValue) query = query.Where(s => s.CreatedAt >= filter.FromUtc);
        if (filter.ToUtcExclusive.HasValue) query = query.Where(s => s.CreatedAt < filter.ToUtcExclusive);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(s => s.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.ReceiptNumber))
        {
            var number = filter.ReceiptNumber.Trim();
            query = query.Where(s => s.ReceiptNumber == number);
        }
        return query;
    }
    public static IOrderedQueryable<Sale> ApplyOrdering(this IQueryable<Sale> query, SaleOrderKey key, bool descending)
    {
        var ordered = key switch
        {
            SaleOrderKey.CompletedAt => descending ? query.OrderByDescending(s => s.CompletedAt) : query.OrderBy(s => s.CompletedAt),
            SaleOrderKey.Total => descending ? query.OrderByDescending(s => s.Total) : query.OrderBy(s => s.Total),
            SaleOrderKey.ReceiptNumber => descending ? query.OrderByDescending(s => s.ReceiptNumber) : query.OrderBy(s => s.ReceiptNumber),
            _ => descending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt)
        };
        return ordered.ThenBy(s => s.Id);
    }
    public static IQueryable<SaleStatusHistory> ApplyFilter(this IQueryable<SaleStatusHistory> query,
        Guid tenantId, Guid saleId, SaleStatusHistoryFilter? filter)
    {
        query = query.Where(h => h.TenantId == tenantId && h.SaleId == saleId);
        if (filter == null) return query;
        if (filter.FromUtc.HasValue) query = query.Where(h => h.ChangedAt >= filter.FromUtc);
        if (filter.ToUtcExclusive.HasValue) query = query.Where(h => h.ChangedAt < filter.ToUtcExclusive);
        if (filter.ChangedByUserId.HasValue) query = query.Where(h => h.ChangedByUserId == filter.ChangedByUserId);
        if (!string.IsNullOrWhiteSpace(filter.NewStatus)) query = query.Where(h => h.NewStatus == filter.NewStatus);
        return query;
    }
    public static IOrderedQueryable<SaleStatusHistory> ApplyOrdering(this IQueryable<SaleStatusHistory> query,
        SaleStatusHistoryOrderKey key, bool descending)
        => descending ? query.OrderByDescending(h => h.ChangedAt).ThenBy(h => h.Id)
            : query.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id);
}

