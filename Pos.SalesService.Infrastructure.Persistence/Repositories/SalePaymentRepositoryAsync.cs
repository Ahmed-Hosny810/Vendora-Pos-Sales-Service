using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Features.SalePayments.Queries.GetAllQuery;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.QueryExtensions;

namespace Pos.SalesService.Infrastructure.Persistence.Repositories;

public class SalePaymentRepositoryAsync : GenericRepositoryAsync<SalePayment, Guid>, ISalePaymentRepositoryAsync
{
    private readonly ApplicationDbContext _context;
    public SalePaymentRepositoryAsync(ApplicationDbContext context) : base(context) { _context = context; }

    public async Task<Sale?> GetSaleForPaymentAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken)
    {
        return await _context.Sales
                         .Include(s => s.Shift)
                         .Include(s => s.Items)
                         .Include(s => s.Payments)
                         .SingleOrDefaultAsync(s => s.TenantId == tenantId && s.Id == saleId, cancellationToken);
    }

    public async Task<SalePayment?> GetByIdempotencyKeyAsync(Guid tenantId, Guid key, CancellationToken cancellationToken)
    {
        return await _context.SalePayments
            .SingleOrDefaultAsync(p => p.TenantId == tenantId && p.IdempotencyKey == key, cancellationToken);
    }

    public void TouchSale(Sale sale)
    {
        sale.UpdatedAt = DateTime.UtcNow;
        // Every payment mutation must contend on the same parent sale row version.
        _context.Entry(sale).Property(s => s.UpdatedAt).IsModified = true;
    }

    public async Task<PagedResponse<IEnumerable<SalePayment>>> GetSalePaymentsPagedAsync(
        Guid tenantId, Guid saleId, SalePaymentFilter? filter, SalePaymentOrderKey orderKey,
        bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);

        var query = _context.SalePayments.AsNoTracking().ApplyFilter(tenantId, saleId, filter);

        var count = await query.CountAsync(cancellationToken);

        var offset = ((long)pageNumber - 1) * pageSize;

        var payments = await query
            .ApplyOrdering(orderKey, descending)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<IEnumerable<SalePayment>>(payments, pageNumber, pageSize, count);
    }
}

