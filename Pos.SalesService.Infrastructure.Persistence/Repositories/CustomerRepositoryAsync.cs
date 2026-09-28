using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Features.Customers.Queries.GetCustomersQuery;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.QueryExtensions;

namespace Pos.SalesService.Infrastructure.Persistence.Repositories;

public class CustomerRepositoryAsync : GenericRepositoryAsync<Customer, Guid>, ICustomerRepositoryAsync
{
    private readonly ApplicationDbContext _context;
    public CustomerRepositoryAsync(ApplicationDbContext context) : base(context) { _context = context; }

    public Task<bool> PhoneExistsAsync(Guid tenantId, string normalizedPhone, CancellationToken cancellationToken)
        => _context.Customers.AnyAsync(x => x.TenantId == tenantId && x.Phone == normalizedPhone, cancellationToken);

    // Tracked lookups are shared by read and update handlers.
    public Task<Customer?> GetCustomerByIdAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken)
        => _context.Customers.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == customerId, cancellationToken);

    public Task<Customer?> GetCustomerByPhoneAsync(Guid tenantId, string normalizedPhone, CancellationToken cancellationToken)
        => _context.Customers.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Phone == normalizedPhone, cancellationToken);

    public async Task<PagedResponse<IEnumerable<Customer>>> GetCustomersPagedAsync(Guid tenantId,
        CustomerFilter? filter, CustomerOrderKey orderKey, bool orderDescending,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);
        var query = _context.Customers.AsNoTracking().ApplyFilter(tenantId, filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var offset = ((long)pageNumber - 1) * pageSize;
        var customers = offset >= totalCount ? new List<Customer>() :
            await query.ApplyOrdering(orderKey, orderDescending)
                .Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResponse<IEnumerable<Customer>>(customers, pageNumber, pageSize, totalCount);
    }
}
