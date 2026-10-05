using Pos.SalesService.Application.Features.RefundPayments.Queries.GetAllQuery;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Infrastructure.Persistence.QueryExtensions;

public static class RefundPaymentQueryExtensions
{
    public static IQueryable<RefundPayment> ApplyFilter(this IQueryable<RefundPayment> query,
        Guid tenantId, Guid returnId, RefundPaymentFilter? filter)
    {
        query = query.Where(x => x.TenantId == tenantId && x.ReturnId == returnId);
        if (filter == null) return query;
        if (filter.PaymentMethodId.HasValue)
            query = query.Where(x => x.PaymentMethodId == filter.PaymentMethodId.Value);
        if (filter.ShiftId.HasValue)
            query = query.Where(x => x.ShiftId == filter.ShiftId.Value);
        if (filter.PaidByUserId.HasValue)
            query = query.Where(x => x.PaidByUserId == filter.PaidByUserId.Value);
        if (filter.IsCash.HasValue)
            query = query.Where(x => x.IsCashSnapshot == filter.IsCash.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.Status == filter.Status);
        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtcExclusive.HasValue)
            query = query.Where(x => x.CreatedAt < filter.ToUtcExclusive.Value);
        return query;
    }

    public static IOrderedQueryable<RefundPayment> ApplyOrdering(this IQueryable<RefundPayment> query,
        RefundPaymentOrderKey orderKey, bool descending)
    {
        var ordered = orderKey switch
        {
            RefundPaymentOrderKey.Amount => descending
                ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            RefundPaymentOrderKey.PaidAt => descending
                ? query.OrderByDescending(x => x.PaidAt) : query.OrderBy(x => x.PaidAt),
            _ => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
        return ordered.ThenBy(x => x.Id);
    }
}

