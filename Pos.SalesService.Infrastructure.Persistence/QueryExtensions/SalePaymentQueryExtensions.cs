using Pos.SalesService.Application.Features.SalePayments.Queries.GetAllQuery;
using Pos.SalesService.Domain.Models;
namespace Pos.SalesService.Infrastructure.Persistence.QueryExtensions;

public static class SalePaymentQueryExtensions
{
    public static IQueryable<SalePayment> ApplyFilter(this IQueryable<SalePayment> query,
        Guid tenantId, Guid saleId, SalePaymentFilter? filter)
    {
        query = query.Where(p => p.TenantId == tenantId && p.SaleId == saleId);
        if (filter == null) return query;
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(p => p.Status == filter.Status);
        if (filter.PaymentMethodId.HasValue)
            query = query.Where(p => p.PaymentMethodId == filter.PaymentMethodId.Value);
        if (filter.IsCash.HasValue)
            query = query.Where(p => p.IsCashSnapshot == filter.IsCash.Value);
        return query;
    }

    public static IOrderedQueryable<SalePayment> ApplyOrdering(this IQueryable<SalePayment> query,
        SalePaymentOrderKey key, bool descending)
    {
        var ordered = key switch
        {
            SalePaymentOrderKey.Amount => descending ? query.OrderByDescending(p => p.Amount) : query.OrderBy(p => p.Amount),
            SalePaymentOrderKey.PaidAt => descending ? query.OrderByDescending(p => p.PaidAt) : query.OrderBy(p => p.PaidAt),
            _ => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };
        return ordered.ThenBy(p => p.Id);
    }
}

