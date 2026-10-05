using Pos.SalesService.Application.Features.Sales.Queries.GetSalesQuery;
using Pos.SalesService.Application.Features.Sales.Queries.GetStatusHistoryQuery;
using Pos.SalesService.Infrastructure.Persistence.QueryExtensions;
using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Exceptions;
using Pos.SalesService.Application.Features.Sales.DTOs.Receipts;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using System.Text.Json;
using Pos.SalesService.Application.Events;

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

        public Task<bool> ExistsAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken)
            => _context.Sales.AnyAsync(s => s.TenantId == tenantId && s.Id == saleId, cancellationToken);

        public async Task<PagedResponse<IEnumerable<Sale>>> GetSalesPagedAsync(Guid tenantId, SaleFilter? filter,
            SaleOrderKey orderKey, bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);
            var query = _context.Sales.AsNoTracking().ApplyFilter(tenantId, filter);
            var count = await query.CountAsync(cancellationToken);
            var offset = ((long)pageNumber - 1) * pageSize;
            var rows = offset >= count ? new List<Sale>() :
                await query.ApplyOrdering(orderKey, descending).Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedResponse<IEnumerable<Sale>>(rows, pageNumber, pageSize, count);
        }

        public async Task<PagedResponse<IEnumerable<SaleStatusHistory>>> GetSaleStatusHistoryPagedAsync(
            Guid tenantId, Guid saleId, SaleStatusHistoryFilter? filter, SaleStatusHistoryOrderKey orderKey,
            bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);
            var query = _context.SaleStatusHistory.AsNoTracking().ApplyFilter(tenantId, saleId, filter);
            var count = await query.CountAsync(cancellationToken);
            var offset = ((long)pageNumber - 1) * pageSize;
            var rows = offset >= count ? new List<SaleStatusHistory>() :
                await query.ApplyOrdering(orderKey, descending).Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedResponse<IEnumerable<SaleStatusHistory>>(rows, pageNumber, pageSize, count);
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

        public async Task<Result<Guid>> IssueReceiptAsync(Guid tenantId,Guid saleId,Guid userId,CancellationToken cancellationToken)
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

                var receiptSequence = await _context.ReceiptSequences.SingleOrDefaultAsync(
                    s => s.TenantId == tenantId &&
                         s.BranchId == sale.BranchId &&
                         s.DocumentType == ReceiptDocumentType.Sale,
                    cancellationToken);

                if (receiptSequence == null)
                    return Result<Guid>.Failure(
                        "Configure a sale receipt sequence for this branch, then retry completion.");

                if (receiptSequence.LastNumber == long.MaxValue)
                    return Result<Guid>.Failure("The receipt sequence has reached its limit.");

                receiptSequence.LastNumber++;

                var receiptNumber = $"{receiptSequence.Prefix}-{receiptSequence.LastNumber:D8}";

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
                var eventId = Guid.NewGuid();

                var saleCompletedEvent = new SaleCompleted(
                    EventId: eventId,
                    TenantId: tenantId,
                    SaleId: sale.Id,
                    TerminalId: sale.TerminalId,
                    BranchId: sale.BranchId,
                    StockReservationId: sale.StockReservationId,
                    CashierUserId: userId,
                    ReceiptNumber: receiptNumber,
                    Subtotal: sale.Subtotal,
                    DiscountTotal: sale.DiscountTotal,
                    TaxTotal: sale.TaxTotal,
                    Total: sale.Total,
                    Items: sale.Items
                        .OrderBy(i => i.ItemNumber)
                        .Select(i => new SaleCompletedItem(
                            ProductId: i.ProductId,
                            ProductVariantId: i.ProductVariantId,
                            Quantity: i.Quantity,
                            UnitPrice: i.UnitPrice,
                            UnitCost: i.UnitCost,
                            DiscountAmount: i.DiscountAmount,
                            TaxAmount: i.TaxAmount,
                            LineTotal: i.LineTotal,
                            TrackInventory: i.TrackInventorySnapshot))
                        .ToList(),
                    OccurredAt: now);

                _context.OutboxMessages.Add(new OutboxMessage
                {
                    Id = eventId,
                    TenantId = tenantId,
                    EventType = nameof(SaleCompleted),
                    Payload = JsonSerializer.Serialize(saleCompletedEvent),
                    OccurredAt = now
                });

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

        public async Task<SaleReceiptDto?> GetSaleReceiptAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken)
        {
            return await _context.Sales
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.Id == saleId &&
                    s.ReceiptNumber != null && s.ReceiptNumber != "" && s.CompletedAt.HasValue &&
                    (s.Status == SaleStatus.Completed ||
                     s.Status == SaleStatus.PartiallyReturned || s.Status == SaleStatus.Returned))
                .Select(s => new SaleReceiptDto
                {
                    SaleId = s.Id,
                    ReceiptNumber = s.ReceiptNumber!,
                    CompletedAt = s.CompletedAt!.Value,
                    BranchId= s.BranchId,
                    CustomerNameSnapshot = s.CustomerNameSnapshot,
                    CustomerPhoneSnapshot = s.CustomerPhoneSnapshot,
                    Subtotal = s.Subtotal,
                    DiscountTotal = s.DiscountTotal,
                    TaxTotal = s.TaxTotal,
                    Total = s.Total,
                    PaidAmount = s.PaidAmount,
                    ChangeAmount = s.ChangeAmount,
                    Items = s.Items.OrderBy(i => i.ItemNumber).ThenBy(i => i.Id).Select(i => new SaleReceiptItemDto
                    {
                        ProductNameSnapshot = i.ProductNameSnapshot,
                        VariantNameSnapshot = i.VariantNameSnapshot,
                        Quantity = i.Quantity,
                        UnitNameSnapshot = i.UnitNameSnapshot,
                        UnitPrice = i.UnitPrice,
                        DiscountAmount = i.DiscountAmount,
                        TaxAmount = i.TaxAmount,
                        LineTotal = i.LineTotal
                    }).ToList(),
                    Payments = s.Payments.Where(p => p.Status == PaymentStatus.Completed)
                        .OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).Select(p => new SaleReceiptPaymentDto
                    {
                        PaymentMethodNameSnapshot = p.PaymentMethodNameSnapshot,
                        IsCashSnapshot = p.IsCashSnapshot,
                        Amount = p.Amount,
                        ChangeAmount = p.ChangeAmount,
                        ReferenceNumber = p.ReferenceNumber
                    }).ToList()
                })
                .SingleOrDefaultAsync(cancellationToken);
        }
    }
}
