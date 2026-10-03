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

    // Expected payment conflicts are returned without introducing exception types.
    // Failed tracked state is discarded before a caller looks up a winning retry.
    public async Task<Pos.SalesService.Application.Wrappers.Result> TrySavePaymentChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Pos.SalesService.Application.Wrappers.Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return Pos.SalesService.Application.Wrappers.Result.Failure(
                "The sale or payment changed while saving. Reload it and retry the same request.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627) &&
            sql.Message.Contains("IX_SalePayments_TenantId_IdempotencyKey", StringComparison.Ordinal))
        {
            _context.ChangeTracker.Clear();
            return Pos.SalesService.Application.Wrappers.Result.Failure(
                "This payment request was already recorded. Retry with the same idempotency key.");
        }
    }
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
    }
}
