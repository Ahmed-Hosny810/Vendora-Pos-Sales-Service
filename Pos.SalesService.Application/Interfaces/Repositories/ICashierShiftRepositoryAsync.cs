using Pos.SalesService.Domain.Models;
using Pos.SalesService.Application.Features.CashierShifts.Queries.GetAllQuery;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Interfaces.Repositories
{
    public interface ICashierShiftRepositoryAsync:IGenericRepositoryAsync<CashierShift,Guid>
    {
        Task<bool> HasOpenedShiftAsync(Guid tenantId, Guid cashierUserId, CancellationToken cancellationToken);
        Task<bool> CanShiftBeClosedAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken);

        Task<decimal> CalculateShiftCashReceiptsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken);
        Task<decimal> CalculateShiftNonCashReceiptsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken);
        Task<decimal> CalculateShiftTotalSalesAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken);
        Task<CashierShift?> GetCurrentCashierShiftAsync(Guid tenantId, Guid cashierUserId, CancellationToken cancellationToken);
        Task<CashierShift?> GetCashierShiftByIdAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken);
        Task<PagedResponse<IEnumerable<CashierShift>>> GetCashierShiftsPagedAsync(Guid tenantId,
            CashierShiftFilter? filter, CashierShiftOrderKey orderKey, bool descending,
            int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<bool> IsTerminalInUseAsync(Guid tenantId,Guid branchId ,Guid terminalId, CancellationToken cancellationToken);
    }
}
