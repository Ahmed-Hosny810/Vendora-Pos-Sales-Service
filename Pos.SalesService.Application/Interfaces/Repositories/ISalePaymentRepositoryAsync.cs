using Pos.SalesService.Application.Features.SalePayments.Queries.GetAllQuery;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Interfaces.Repositories;

public interface ISalePaymentRepositoryAsync : IGenericRepositoryAsync<SalePayment, Guid>
{
    Task<Sale?> GetSaleForPaymentAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken);
    Task<SalePayment?> GetByIdempotencyKeyAsync(Guid tenantId, Guid key, CancellationToken cancellationToken);
    void TouchSale(Sale sale);
    Task<PagedResponse<IEnumerable<SalePayment>>> GetSalePaymentsPagedAsync(
        Guid tenantId, Guid saleId, SalePaymentFilter? filter, SalePaymentOrderKey orderKey,
        bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken);
}

