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

public class SaleReturnRepositoryAsync : GenericRepositoryAsync<SaleReturn, Guid>, ISaleReturnRepositoryAsync
{
    private readonly ApplicationDbContext _context;
    public SaleReturnRepositoryAsync(ApplicationDbContext context) : base(context) { _context = context; }

    public Task<SaleReturn?> GetByIdAsync(Guid tenantId, Guid returnId, CancellationToken cancellationToken)
    {
        return _context.Returns.Include(x => x.Items).ThenInclude(x => x.OriginalSaleItem)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == returnId, cancellationToken);
    }

    public Task<Sale?> GetOriginalSaleAsync(Guid tenantId, Guid saleId, CancellationToken cancellationToken)
    {
        
        return _context.Sales.AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(x => x.ReturnItems.Where(r => r.Return.Status == SaleReturnStatus.Completed))
            .AsSplitQuery()
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
                            .Where(r => r.Return.Status == SaleReturnStatus.Completed)
                            .Sum(r => (decimal?)r.RefundAmount) ?? 0m),
                        RemainingTaxAmount = x.TaxAmount - (x.ReturnItems
                            .Where(r => r.Return.Status == SaleReturnStatus.Completed)
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

    public void ReplaceItems(SaleReturn saleReturn, IReadOnlyList<SaleReturnItem> items)
    {
        _context.ReturnItems.RemoveRange(saleReturn.Items);
        saleReturn.Items.Clear();

        foreach (var item in items)
            saleReturn.Items.Add(item);
    }

    public void MarkUpdated(SaleReturn saleReturn)
    {
        // Item-only changes must also check and advance the draft's row version.
        saleReturn.UpdatedAt = DateTime.UtcNow;
        _context.Entry(saleReturn).Property(x => x.UpdatedAt).IsModified = true;
    }
}
