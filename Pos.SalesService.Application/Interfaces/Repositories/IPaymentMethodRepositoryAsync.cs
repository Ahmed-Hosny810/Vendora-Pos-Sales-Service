using Pos.SalesService.Application.Features.PaymentMethods.Queries.GetAllQuery;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;
namespace Pos.SalesService.Application.Interfaces.Repositories;
public interface IPaymentMethodRepositoryAsync : IGenericRepositoryAsync<PaymentMethod, Guid>
{
    Task<PaymentMethod?> GetPaymentMethodByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<PaymentMethod?> GetPaymentMethodByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken);
    Task<bool> HasBeenUsedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<IEnumerable<PaymentMethod>>> GetPaymentMethodsPagedAsync(Guid tenantId,
        PaymentMethodFilter? filter, PaymentMethodOrderKey orderKey, bool descending,
        int pageNumber, int pageSize, CancellationToken cancellationToken);
}
