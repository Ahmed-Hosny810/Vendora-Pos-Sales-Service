using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Features.PaymentMethods.Queries.GetAllQuery;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.QueryExtensions;
namespace Pos.SalesService.Infrastructure.Persistence.Repositories;
public class PaymentMethodRepositoryAsync : GenericRepositoryAsync<PaymentMethod, Guid>, IPaymentMethodRepositoryAsync
{
    private readonly ApplicationDbContext _context;
    public PaymentMethodRepositoryAsync(ApplicationDbContext context) : base(context) { _context = context; }
    public Task<PaymentMethod?> GetPaymentMethodByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
        => _context.PaymentMethods.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken);
    public Task<PaymentMethod?> GetPaymentMethodByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken)
        => _context.PaymentMethods.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, cancellationToken);
    public async Task<bool> HasBeenUsedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        return await _context.SalePayments.AnyAsync(
            x => x.TenantId == tenantId && x.PaymentMethodId == id, cancellationToken)
            || await _context.RefundPayments.AnyAsync(
                x => x.TenantId == tenantId && x.PaymentMethodId == id, cancellationToken);
    }
    public async Task<PagedResponse<IEnumerable<PaymentMethod>>> GetPaymentMethodsPagedAsync(Guid tenantId,
        PaymentMethodFilter? filter, PaymentMethodOrderKey orderKey, bool descending,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);
        var query = _context.PaymentMethods.AsNoTracking().ApplyFilter(tenantId, filter);
        var count = await query.CountAsync(cancellationToken);
        var offset = ((long)pageNumber - 1) * pageSize;
        var rows = offset >= count ? new List<PaymentMethod>() :
            await query.ApplyOrdering(orderKey, descending).Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResponse<IEnumerable<PaymentMethod>>(rows, pageNumber, pageSize, count);
    }
}
