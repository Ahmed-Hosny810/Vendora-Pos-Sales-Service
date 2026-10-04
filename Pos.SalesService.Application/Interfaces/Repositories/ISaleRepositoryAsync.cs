using Pos.SalesService.Application.Features.Sales.Queries.GetSalesQuery;
using Pos.SalesService.Application.Features.Sales.Queries.GetStatusHistoryQuery;

using Pos.SalesService.Application.Features.Sales.DTOs.Receipts;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Interfaces.Repositories
{
    public interface ISaleRepositoryAsync:IGenericRepositoryAsync<Sale,Guid>
    {
        Task<bool> ExistsAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken);
        Task<PagedResponse<IEnumerable<Sale>>> GetSalesPagedAsync(Guid tenantId, SaleFilter? filter,
            SaleOrderKey orderKey, bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<PagedResponse<IEnumerable<SaleStatusHistory>>> GetSaleStatusHistoryPagedAsync(
            Guid tenantId, Guid saleId, SaleStatusHistoryFilter? filter, SaleStatusHistoryOrderKey orderKey,
            bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<Sale?> GetByIdAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken);
        Task SaveDraftAsync(Sale sale, IReadOnlyList<SaleItem> items, CancellationToken cancellationToken);
        Task<Sale?> GetByIdempotencyKeyAsync(Guid tenantId,Guid idempotencyKey,CancellationToken cancellationToken);
        void RemoveDiscount(Sale sale, SaleDiscount discount);

        Task<SaleReceiptDto?> GetSaleReceiptAsync(Guid tenantId,Guid saleId,CancellationToken cancellationToken);

        /// <summary>
        /// Final step of sale completion: allocates the next receipt number for the
        /// branch, transitions the sale from Completing to Completed, and stages the
        /// SaleCompleted event — all in one transaction, with its own retry for
        /// contention on the shared receipt sequence counter.
        ///
        /// Must only be called after the sale's stock reservation has already been
        /// confirmed consumed (or the sale has no tracked items). The receipt number
        /// is irreversible once assigned, so this method never runs ahead of that
        /// confirmation — see CompleteSaleCommandHandler for the ordering.
        ///
        /// Idempotent: if the sale is already Completed, returns its existing
        /// receipt without allocating a new number or re-staging the event.
        /// </summary>
        Task<Result<Guid>> IssueReceiptAsync(Guid tenantId,Guid saleId,Guid userId,CancellationToken cancellationToken);
    }
}
