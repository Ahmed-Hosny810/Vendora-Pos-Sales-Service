using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
namespace Pos.SalesService.Infrastructure.Persistence.Repositories
{
    using Pos.SalesService.Application.Features.RefundPayments.Queries.GetAllQuery;
    using Pos.SalesService.Application.Wrappers;
    using Pos.SalesService.Domain.Constants;
    using Pos.SalesService.Infrastructure.Persistence.QueryExtensions;
    public class RefundPaymentRepositoryAsync: GenericRepositoryAsync<RefundPayment, Guid>,IRefundPaymentRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public RefundPaymentRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<bool> ReturnExistsAsync(Guid tenantId, Guid returnId, CancellationToken cancellationToken)
        {
            return _context.Returns.AnyAsync(
                x => x.TenantId == tenantId && x.Id == returnId, cancellationToken);
        }

        public async Task<PagedResponse<IEnumerable<RefundPayment>>> GetRefundPaymentsPagedAsync(
            Guid tenantId, Guid returnId, RefundPaymentFilter? filter, RefundPaymentOrderKey orderKey,
            bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            pageNumber = Math.Max(1, pageNumber);

            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.RefundPayments
                .AsNoTracking()
                .ApplyFilter(tenantId, returnId, filter);

            var totalCount = await query.CountAsync(cancellationToken);

            var payments = await query
                .ApplyOrdering(orderKey, descending)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponse<IEnumerable<RefundPayment>>(payments, pageNumber, pageSize, totalCount);
        }

        

        public Task<RefundPayment?> GetByIdempotencyKeyAsync(
            Guid tenantId,
            Guid idempotencyKey,
            CancellationToken cancellationToken)
        {
            return _context.RefundPayments.SingleOrDefaultAsync(
                payment =>
                    payment.TenantId == tenantId &&
                    payment.IdempotencyKey == idempotencyKey,
                cancellationToken);
        }

        public Task<SaleReturn?> GetReturnForRefundAsync(
            Guid tenantId,
            Guid returnId,
            CancellationToken cancellationToken)
        {
            return _context.Returns
                .Include(saleReturn => saleReturn.RefundPayments)
                .SingleOrDefaultAsync(
                    saleReturn =>
                        saleReturn.TenantId == tenantId &&
                        saleReturn.Id == returnId,
                    cancellationToken);
        }

        public void MarkReturnAndShiftForUpdate(
            SaleReturn saleReturn,
            CashierShift shift)
        {
            //Prevent concurrent refunds exceeding the authorized amount.
            _context.Entry(saleReturn)
                .Property(x => x.RefundAmount)
                .IsModified = true;

            // Prevent recording against a shift closed after it was loaded.
            shift.UpdatedAt = DateTime.UtcNow;

            _context.Entry(shift)
                .Property(x => x.UpdatedAt)
                .IsModified = true;

        }

        public async Task<decimal> CalculateShiftCashRefundsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken)
        {
            return await _context.RefundPayments
                 .Where(refund =>
                     refund.TenantId == tenantId &&
                     refund.ShiftId == shiftId &&  
                     refund.Status == PaymentStatus.Completed &&
                     refund.IsCashSnapshot)
                 .SumAsync(refund => (decimal?)refund.Amount, cancellationToken)
                 ?? 0m;
        }
    }
}
