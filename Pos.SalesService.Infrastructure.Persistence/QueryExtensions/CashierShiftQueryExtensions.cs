using Pos.SalesService.Application.Features.CashierShifts.Queries.GetAllQuery;
using Pos.SalesService.Domain.Models;
namespace Pos.SalesService.Infrastructure.Persistence.QueryExtensions;
public static class CashierShiftQueryExtensions
{
    public static IQueryable<CashierShift> ApplyFilter(this IQueryable<CashierShift> query,
        Guid tenantId, CashierShiftFilter? filter)
    {
        query = query.Where(x => x.TenantId == tenantId);
        if (filter == null) return query;
        if (filter.BranchId.HasValue) query = query.Where(x => x.BranchId == filter.BranchId.Value);
        if (filter.TerminalId.HasValue) query = query.Where(x => x.TerminalId == filter.TerminalId.Value);
        if (filter.CashierUserId.HasValue) query = query.Where(x => x.CashierUserId == filter.CashierUserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(x => x.Status == filter.Status);
        if (filter.FromUtc.HasValue) query = query.Where(x => x.OpenedAt >= filter.FromUtc.Value);
        if (filter.ToUtcExclusive.HasValue) query = query.Where(x => x.OpenedAt < filter.ToUtcExclusive.Value);
        return query;
    }
    public static IOrderedQueryable<CashierShift> ApplyOrdering(this IQueryable<CashierShift> query,
        CashierShiftOrderKey key, bool descending)
    {
        var ordered = key switch
        {
            CashierShiftOrderKey.ClosedAt => descending ? query.OrderByDescending(x => x.ClosedAt) : query.OrderBy(x => x.ClosedAt),
            CashierShiftOrderKey.OpeningCash => descending ? query.OrderByDescending(x => x.OpeningCash) : query.OrderBy(x => x.OpeningCash),
            _ => descending ? query.OrderByDescending(x => x.OpenedAt) : query.OrderBy(x => x.OpenedAt)
        };
        return ordered.ThenBy(x => x.Id);
    }
}
