using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Infrastructure.Persistence.Contexts;

namespace Pos.SalesService.Infrastructure.Persistence.UnitofWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public UnitOfWork(ApplicationDbContext context) { _context = context; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The record changed while saving.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627) &&
            sql.Message.Contains("IX_PaymentMethods_TenantId_Code", StringComparison.Ordinal))
        {
            throw new DuplicatePaymentMethodCodeException(
                "A payment method with this code already exists.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627) &&
            sql.Message.Contains("IX_Customers_TenantId_Phone", StringComparison.Ordinal))
        {
            throw new DuplicateCustomerPhoneException(
                "A customer with this phone number already exists.", exception);
        }
    }
}
