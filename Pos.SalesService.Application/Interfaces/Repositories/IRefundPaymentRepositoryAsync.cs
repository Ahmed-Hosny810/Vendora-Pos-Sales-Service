using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Interfaces.Repositories
{
    using Pos.SalesService.Application.Features.RefundPayments.Queries.GetAllQuery;
    using Pos.SalesService.Application.Wrappers;
    public interface IRefundPaymentRepositoryAsync : IGenericRepositoryAsync<RefundPayment, Guid>
    {
        Task<bool> ReturnExistsAsync(Guid tenantId, Guid returnId, CancellationToken cancellationToken);
        Task<PagedResponse<IEnumerable<RefundPayment>>> GetRefundPaymentsPagedAsync(
            Guid tenantId, Guid returnId, RefundPaymentFilter? filter, RefundPaymentOrderKey orderKey,
            bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<RefundPayment?> GetByIdempotencyKeyAsync(
            Guid tenantId,
            Guid idempotencyKey,
            CancellationToken cancellationToken);

        Task<decimal> CalculateShiftCashRefundsAsync(Guid tenantId,Guid shiftId,CancellationToken cancellationToken);

        Task<SaleReturn?> GetReturnForRefundAsync(
            Guid tenantId,
            Guid returnId,
            CancellationToken cancellationToken);

        void MarkReturnAndShiftForUpdate(
            SaleReturn saleReturn,
            CashierShift shift);
    }
}
