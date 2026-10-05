using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Infrastructure.Persistence.UnitofWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    public UnitOfWork(ApplicationDbContext context) { _context = context; }

    public async Task<Result> TrySaveReturnChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return Result.Failure(
                "The original sale changed while completing the return. Reload it and retry with the same idempotency key.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627) &&
            sql.Message.Contains("IX_Returns_TenantId_IdempotencyKey", StringComparison.Ordinal))
        {
            _context.ChangeTracker.Clear();
            return Result.Failure(
                "This return key was used by another request. Retry with the same idempotency key.");
        }
    }

    // Expected payment conflicts are returned without introducing exception types.
    // Failed tracked state is discarded before a caller looks up a winning retry.
    public async Task<Result> TrySavePaymentChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return Result.Failure(
                "The sale or payment changed while saving. Reload it and retry the same request.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627) &&
            sql.Message.Contains("IX_SalePayments_TenantId_IdempotencyKey", StringComparison.Ordinal))
        {
            _context.ChangeTracker.Clear();
            return Result.Failure(
                "This payment request was already recorded. Retry with the same idempotency key.");
        }
    }

    public async Task<Result>TrySaveRefundChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();

            return Result.Failure(
                "The return, shift or payment method changed. " +
                "Reload and retry recording with the same idempotency key. " +
                "Do not pay the customer again.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627) &&
            sql.Message.Contains(
                "IX_RefundPayments_TenantId_IdempotencyKey",
                StringComparison.Ordinal))
        {
            _context.ChangeTracker.Clear();

            return Result.Failure(
                "This refund request was recorded concurrently. " +
                "Retry with the same idempotency key.");
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
