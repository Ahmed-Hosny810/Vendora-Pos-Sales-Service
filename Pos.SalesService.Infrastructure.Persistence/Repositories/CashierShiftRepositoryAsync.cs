using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Features.CashierShifts.Queries.GetAllQuery;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Infrastructure.Persistence.QueryExtensions;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;

namespace Pos.SalesService.Infrastructure.Persistence.Repositories
{
    public class CashierShiftRepositoryAsync : GenericRepositoryAsync<CashierShift, Guid>, ICashierShiftRepositoryAsync
    {
        private readonly ApplicationDbContext _context;

        public Task<CashierShift?> GetCurrentCashierShiftAsync(Guid tenantId, Guid cashierUserId,
            CancellationToken cancellationToken)
        {
            return _context.CashierShifts.SingleOrDefaultAsync(x => x.TenantId == tenantId &&
                x.CashierUserId == cashierUserId && x.Status == CashierShiftStatus.Open, cancellationToken);
        }

        public Task<CashierShift?> GetCashierShiftByIdAsync(Guid tenantId, Guid shiftId,
            CancellationToken cancellationToken)
        {
            return _context.CashierShifts.SingleOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == shiftId, cancellationToken);
        }

        public async Task<PagedResponse<IEnumerable<CashierShift>>> GetCashierShiftsPagedAsync(
            Guid tenantId, CashierShiftFilter? filter, CashierShiftOrderKey orderKey, bool descending,
            int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);
            var query = _context.CashierShifts.AsNoTracking().ApplyFilter(tenantId, filter);
            var count = await query.CountAsync(cancellationToken);
            var offset = ((long)pageNumber - 1) * pageSize;
            var rows = offset >= count ? new List<CashierShift>() :
                await query.ApplyOrdering(orderKey, descending)
                    .Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedResponse<IEnumerable<CashierShift>>(rows, pageNumber, pageSize, count);
        }

        public CashierShiftRepositoryAsync(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> HasOpenedShiftAsync(Guid tenantId, Guid cashierUserId, CancellationToken cancellationToken)
        {
            return await _context.CashierShifts
                .AnyAsync(cs => cs.TenantId == tenantId && cs.CashierUserId == cashierUserId &&
                    cs.Status == "Open", cancellationToken);
        }

        public async Task<bool> IsTerminalInUseAsync(Guid tenantId, Guid branchId, Guid terminalId, CancellationToken cancellationToken)
        {
            return await _context.CashierShifts
                .AnyAsync(cs => cs.TenantId == tenantId && cs.BranchId == branchId &&
                    cs.TerminalId == terminalId && cs.Status == "Open", cancellationToken);
        }

        public async Task<bool> CanShiftBeClosedAsync(
            Guid tenantId,
            Guid shiftId,
            CancellationToken cancellationToken)
        {
            return await _context.CashierShifts
                .AnyAsync(
                    shift =>
                        shift.TenantId == tenantId &&
                        shift.Id == shiftId &&
                        shift.Status == CashierShiftStatus.Open &&
                        !shift.Sales.Any(sale =>
                            sale.Status == SaleStatus.CheckoutPending ||
                            sale.Status == SaleStatus.PendingPayment ||
                            sale.Status == SaleStatus.Completing),
                    cancellationToken);
        }

        public async Task<decimal> CalculateShiftCashReceiptsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken)
        {
            return await _context.SalePayments
                 .Where(payment =>
                     payment.TenantId == tenantId &&
                     payment.Sale.ShiftId == shiftId &&
                     payment.Status == PaymentStatus.Completed &&
                     payment.IsCashSnapshot)
                 .SumAsync(payment => (decimal?)(payment.Amount - payment.ChangeAmount), cancellationToken)
                 ?? 0m;
        }

        public async Task<decimal> CalculateShiftNonCashReceiptsAsync(Guid tenantId,Guid shiftId,CancellationToken cancellationToken)
        {
            return await _context.SalePayments
                .Where(payment =>
                    payment.TenantId == tenantId &&
                    payment.Sale.TenantId == tenantId &&
                    payment.Sale.ShiftId == shiftId &&
                    payment.Status == PaymentStatus.Completed &&
                    !payment.IsCashSnapshot)
                .SumAsync(
                    payment => (decimal?)(payment.Amount - payment.ChangeAmount),
                    cancellationToken)
                ?? 0m;
        }


        public async Task<decimal> CalculateShiftTotalSalesAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken)
        {
            return await _context.Sales
                .Where(sale =>
                    sale.TenantId == tenantId &&
                    sale.ShiftId == shiftId &&
                    (sale.Status == SaleStatus.Completed ||
                     sale.Status == SaleStatus.PartiallyReturned ||
                     sale.Status == SaleStatus.Returned))
                .SumAsync(
                    sale => (decimal?)sale.Total,
                    cancellationToken)
                ?? 0m;
        }
    }
}
