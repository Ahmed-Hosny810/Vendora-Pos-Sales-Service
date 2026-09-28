using Pos.SalesService.Application.Features.Customers.Queries.GetCustomersQuery;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Interfaces.Repositories;

public interface ICustomerRepositoryAsync : IGenericRepositoryAsync<Customer, Guid>
{
    Task<bool> PhoneExistsAsync(Guid tenantId, string normalizedPhone, CancellationToken cancellationToken);
    Task<Customer?> GetCustomerByIdAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken);
    Task<Customer?> GetCustomerByPhoneAsync(Guid tenantId, string normalizedPhone, CancellationToken cancellationToken);
    Task<PagedResponse<IEnumerable<Customer>>> GetCustomersPagedAsync(Guid tenantId,
        CustomerFilter? filter, CustomerOrderKey orderKey, bool orderDescending,
        int pageNumber, int pageSize, CancellationToken cancellationToken);
}
