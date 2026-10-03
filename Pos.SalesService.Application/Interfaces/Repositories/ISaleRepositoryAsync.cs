
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Interfaces.Repositories
{
    public interface ISaleRepositoryAsync:IGenericRepositoryAsync<Sale,Guid>
    {
        Task<Sale?> GetByIdAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken);
        Task SaveDraftAsync(Sale sale, IReadOnlyList<SaleItem> items, CancellationToken cancellationToken);
        Task<Sale?> GetByIdempotencyKeyAsync(Guid tenantId,Guid idempotencyKey,CancellationToken cancellationToken);
        void RemoveDiscount(Sale sale, SaleDiscount discount);

    }
}
