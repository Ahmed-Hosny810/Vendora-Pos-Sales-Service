using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Features.SalesReturns.Queries.GetAllQuery;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.QueryExtensions;

namespace Pos.SalesService.Infrastructure.Persistence.Repositories;

using Pos.SalesService.Application.Features.SalesReturns.DTOs.Receipts;

public class SaleReturnRepositoryAsync : GenericRepositoryAsync<SaleReturn, Guid>, ISaleReturnRepositoryAsync
{
    private readonly ApplicationDbContext _context;

    public SaleReturnRepositoryAsync(ApplicationDbContext context) : base(context) 
    {
        _context = context;
    }


    public Task<SaleReturnReceiptDto?> GetSaleReturnReceiptAsync(
        Guid tenantId, Guid returnId, CancellationToken cancellationToken)
    {
        return _context.Returns
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == returnId)
            .Select(x => new SaleReturnReceiptDto
            {
                ReturnId = x.Id,
                ReturnNumber = x.ReturnNumber,
                CompletedAt = x.CompletedAt,
                OriginalSaleId = x.OriginalSaleId,
                OriginalReceiptNumber = x.OriginalSale.ReceiptNumber,
                BranchId = x.BranchId,
                CurrencyCode = x.OriginalSale.CurrencyCode,
                CustomerNameSnapshot = x.OriginalSale.CustomerNameSnapshot,
                CustomerPhoneSnapshot = x.OriginalSale.CustomerPhoneSnapshot,
                Reason = x.Reason,
                RefundAmount = x.RefundAmount,
                Items = x.Items.OrderBy(i => i.OriginalSaleItem.ItemNumber).ThenBy(i => i.Id)
                    .Select(i => new SaleReturnReceiptItemDto
                    {
                        OriginalItemNumber = i.OriginalSaleItem.ItemNumber,
                        ProductNameSnapshot = i.OriginalSaleItem.ProductNameSnapshot,
                        VariantNameSnapshot = i.OriginalSaleItem.VariantNameSnapshot,
                        UnitNameSnapshot = i.OriginalSaleItem.UnitNameSnapshot,
                        Quantity = i.Quantity,
                        StockCondition = i.StockCondition,
                        Restock = i.Restock,
                        TaxAmount = i.TaxAmount,
                        RefundAmount = i.RefundAmount
                    }).ToList(),
                Payments = x.RefundPayments.Where(p => p.Status == PaymentStatus.Completed)
                    .OrderBy(p => p.PaidAt).ThenBy(p => p.Id)
                    .Select(p => new SaleReturnReceiptPaymentDto
                    {
                        PaymentMethodNameSnapshot = p.PaymentMethodNameSnapshot,
                        PaymentMethodCodeSnapshot = p.PaymentMethodCodeSnapshot,
                        IsCashSnapshot = p.IsCashSnapshot,
                        Amount = p.Amount,
                        ReferenceNumber = p.ReferenceNumber,
                        PaidAt = p.PaidAt
                    }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<SaleReturn?> GetByIdAsync(Guid tenantId, Guid returnId, CancellationToken cancellationToken)
    {
        return _context.Returns.Include(x => x.Items).ThenInclude(x => x.OriginalSaleItem)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == returnId, cancellationToken);
    }

    public Task<Sale?> GetOriginalSaleAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken)
    {
        
        return _context.Sales
            .Include(x => x.Items)
            .ThenInclude(x => x.ReturnItems)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == saleId, cancellationToken);
    }

    public Task<ReturnableSaleDto?> GetReturnableSaleItemsAsync(
        Guid tenantId, Guid saleId, CancellationToken cancellationToken)
    {
        return _context.Sales
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Id == saleId)
            .Select(s => new ReturnableSaleDto
            {
                Status = s.Status,
                Items = s.Items.Where(x => x.Quantity > x.ReturnedQuantity)
                    .OrderBy(x => x.ItemNumber)
                    .Select(x => new ReturnableSaleItemDto
                    {
                        OriginalSaleItemId = x.Id,
                        ItemNumber = x.ItemNumber,
                        ProductId = x.ProductId,
                        ProductVariantId = x.ProductVariantId,
                        ProductNameSnapshot = x.ProductNameSnapshot,
                        VariantNameSnapshot = x.VariantNameSnapshot,
                        UnitNameSnapshot = x.UnitNameSnapshot,
                        TrackInventorySnapshot = x.TrackInventorySnapshot,
                        OriginalQuantity = x.Quantity,
                        ReturnedQuantity = x.ReturnedQuantity,
                        RemainingQuantity = x.Quantity - x.ReturnedQuantity,
                        RemainingRefundAmount = x.LineTotal - (x.ReturnItems
                            .Sum(r => (decimal?)r.RefundAmount) ?? 0m),
                        RemainingTaxAmount = x.TaxAmount - (x.ReturnItems
                            .Sum(r => (decimal?)r.TaxAmount) ?? 0m)
                    }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<IEnumerable<SaleReturn>>> GetPagedAsync(
        Guid tenantId, SaleReturnFilter? filter, SaleReturnOrderKey orderKey,
        bool descending, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Returns.AsNoTracking().ApplyFilter(tenantId, filter);

        var count = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplyOrdering(orderKey, descending).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<IEnumerable<SaleReturn>>(items, pageNumber, pageSize, count);
    }

    public void MarkOriginalSaleUpdated(Sale sale)
    {
        // Serialize returns even when different sale items are being returned.
        sale.UpdatedAt = DateTime.UtcNow;
        _context.Entry(sale).Property(x => x.UpdatedAt).IsModified = true;
    }

    public async Task<SaleReturn?> GetByIdempotencyKeyAsync(Guid tenantId, Guid idempotencyKey, CancellationToken cancellationToken)
    {
        return await _context.Returns
                .Include(r => r.Items)
                .SingleOrDefaultAsync(r=>r.TenantId==tenantId && r.IdempotencyKey == idempotencyKey,cancellationToken);
    }
}
