using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using System.Text.Json;

namespace Pos.SalesService.Infrastructure.Persistence.Repositories
{
    public class SaleRepositoryAsync : GenericRepositoryAsync<Sale, Guid>, ISaleRepositoryAsync
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public SaleRepositoryAsync(ApplicationDbContext context, IUnitOfWork unitOfWork) : base(context)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<Sale?> GetByIdempotencyKeyAsync(Guid tenantId, Guid idempotencyKey, CancellationToken cancellationToken)
        {
            return await _context.Sales.SingleOrDefaultAsync(
                s => s.TenantId == tenantId && s.IdempotencyKey == idempotencyKey, cancellationToken);
        }

        public async Task<Sale?> GetByIdAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken)
        {
            return await _context.Sales
                .Include(s => s.Shift)
                .Include(s => s.Items)
                .Include(s => s.Discounts)
                .SingleOrDefaultAsync(s => s.TenantId == tenantId && s.Id == saleId, cancellationToken);
        }

        public void RemoveDiscount(Sale sale, SaleDiscount discount)
        {
            _context.SaleDiscounts.Remove(discount);

            sale.Discounts.Remove(discount);

            // Ensure the parent sale's row version is checked.
            _context.Entry(sale).Property(s => s.UpdatedAt).IsModified = true;
        }

        public async Task SaveDraftAsync(
             Sale sale,
             IReadOnlyList<SaleItem> items,
             CancellationToken cancellationToken)
        {
            var requestedIds = items.Select(i => i.Id).ToHashSet();
            var existingItems = sale.Items.ToDictionary(i => i.Id);

            // Remove items omitted from the request.
            foreach (var removed in sale.Items
                .Where(i => !requestedIds.Contains(i.Id))
                .ToList())
            {
                _context.SaleItems.Remove(removed);
                sale.Items.Remove(removed);
            }

            // Add new items and update retained items.
            foreach (var prepared in items)
            {
                if (!existingItems.TryGetValue(prepared.Id, out var item))
                {
                    sale.Items.Add(prepared);
                    _context.SaleItems.Add(prepared);
                }
                else
                {
                    // Preserve the existing item number and snapshots.
                    item.Quantity = prepared.Quantity;
                    item.DiscountAmount = prepared.DiscountAmount;
                    item.TaxAmount = prepared.TaxAmount;
                    item.LineTotal = prepared.LineTotal;
                }
            }

            // Check the sale's row version even when only its items changed.
            _context.Entry(sale).Property(s => s.UpdatedAt).IsModified = true;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<Result<Guid>> FinalizeWithReceiptAsync(Guid tenantId,Guid saleId,Guid userId,CancellationToken cancellationToken)
        {
            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
               
                _context.ChangeTracker.Clear();

                var sale = await _context.Sales
                    .Include(s => s.Items)
                    .SingleOrDefaultAsync(s => s.TenantId == tenantId && s.Id == saleId, cancellationToken);

                if (sale == null)
                    return Result<Guid>.Failure("Sale was not found.");

                if (sale.Status == SaleStatus.Completed)
                    return Result<Guid>.Success(sale.Id);

                if (sale.Status != SaleStatus.Completing)
                    return Result<Guid>.Failure("The sale is no longer awaiting completion.");

                var sequence = await _context.ReceiptSequences.SingleOrDefaultAsync(
                    s => s.TenantId == tenantId &&
                         s.BranchId == sale.BranchId &&
                         s.DocumentType == ReceiptDocumentType.Sale,
                    cancellationToken);

                if (sequence == null)
                    return Result<Guid>.Failure(
                        "Configure a sale receipt sequence for this branch, then retry completion.");

                if (sequence.LastNumber == long.MaxValue)
                    return Result<Guid>.Failure("The receipt sequence has reached its limit.");

                sequence.LastNumber++;

                var receiptNumber = $"{sequence.Prefix}-{sequence.LastNumber:D8}";

                var now = DateTime.UtcNow;

                sale.ReceiptNumber = receiptNumber;
                sale.Status = SaleStatus.Completed;
                sale.CompletedAt = now;
                sale.UpdatedAt = now;

                sale.StatusHistory.Add(new SaleStatusHistory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SaleId = sale.Id,
                    OldStatus = SaleStatus.Completing,
                    NewStatus = SaleStatus.Completed,
                    ChangedByUserId = userId,
                    ChangedAt = now,
                    Reason = "Sale completed and receipt issued."
                });

                // Stage the event in the same transaction as the status change.
                // If SaveChangesAsync below fails, this row is never committed either.
                //var eventId = Guid.NewGuid();

                //var saleCompletedEvent = new SaleCompleted(
                //    eventId,
                //    tenantId,
                //    sale.Id,
                //    sale.BranchId,
                //    receiptNumber,
                //    sale.Total,
                //    sale.Items.Select(i => new SaleCompletedItem(
                //        i.ProductId, i.ProductVariantId, i.Quantity, i.TrackInventorySnapshot)).ToList(),
                //    now);

                //_context.OutboxMessages.Add(new OutboxMessage
                //{
                //    Id = eventId,
                //    TenantId = tenantId,
                //    EventType = nameof(SaleCompleted),
                //    Payload = JsonSerializer.Serialize(saleCompletedEvent),
                //    OccurredAt = now
                //});

                try
                {
                    // The counter, sale, history and outbox event commit or roll back together.
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result<Guid>.Success(sale.Id);
                }
                catch (ConcurrencyConflictException)
                {
                    // The next iteration reloads the sale, receipt counter and rebuilds the event.
                    _context.ChangeTracker.Clear();
                }
            }

            return Result<Guid>.Failure(
                "The receipt could not be allocated because of concurrent updates. " +
                "Retry completion; do not collect payment again.");
        }
    }
}