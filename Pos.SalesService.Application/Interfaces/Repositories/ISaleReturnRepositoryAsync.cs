using Pos.SalesService.Application.Features.SalesReturns.Queries.GetAllQuery;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Interfaces.Repositories;

using Pos.SalesService.Application.Features.SalesReturns.DTOs.Receipts;

public interface ISaleReturnRepositoryAsync : IGenericRepositoryAsync<SaleReturn, Guid>
{
    Task<SaleReturnReceiptDto?> GetSaleReturnReceiptAsync(Guid tenantId, Guid returnId, CancellationToken cancellationToken);
    Task<SaleReturn?> GetByIdAsync(Guid tenantId, Guid returnId, CancellationToken cancellationToken);
    Task<SaleReturn?> GetByIdempotencyKeyAsync(Guid tenantId, Guid idempotencyKey, CancellationToken cancellationToken);
    Task<Sale?> GetOriginalSaleAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken);
    Task<ReturnableSaleDto?> GetReturnableSaleItemsAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken);
    Task<PagedResponse<IEnumerable<SaleReturn>>> GetPagedAsync(
        Guid tenantId, SaleReturnFilter? filter, SaleReturnOrderKey orderKey,
        bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken);
    void MarkOriginalSaleUpdated(Sale sale);
}
