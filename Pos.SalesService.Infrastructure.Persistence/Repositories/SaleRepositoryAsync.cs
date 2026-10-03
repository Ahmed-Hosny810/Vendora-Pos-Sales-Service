using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Interfaces;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;

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
    }
}