using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.DTOs;
using Pos.SalesService.Application.DTOS;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.ReadModels;

namespace Pos.SalesService.Infrastructure.Persistence.Services;

/// <inheritdoc />
public class SalesValidationService : ISalesValidationService
{
    private const string Active = "Active";
    private readonly ApplicationDbContext _context;

    public SalesValidationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> ValidateBranchAsync(Guid branchId, CancellationToken cancellationToken)
    {
        var tenantId = _context.CurrentTenantId;
        var exists = await _context.Set<SalesBranchReadModel>().AnyAsync(
            x => x.Id == branchId && x.TenantId == tenantId && x.Status == Active, cancellationToken);

        return exists ? Result.Success() : Result.Failure("Branch was not found or is inactive.");
    }

    public async Task<Result> ValidateTerminalAsync(Guid branchId, Guid terminalId,
        CancellationToken cancellationToken)
    {
        var branchResult = await ValidateBranchAsync(branchId, cancellationToken);
        if (branchResult.IsFailure)
            return branchResult;

        var tenantId = _context.CurrentTenantId;

        var exists = await _context.Set<SalesTerminalReadModel>().AnyAsync(
            x => x.Id == terminalId && x.TenantId == tenantId &&
                 x.BranchId == branchId && x.Status == Active, cancellationToken);

        return exists ? Result.Success() :
            Result.Failure("Terminal was not found, is inactive or belongs to another branch.");
    }

    public async Task<Result<ValidatedSaleItemDto>> ValidateSaleItemAsync(
        Guid productId, Guid? productVariantId, decimal quantity, CancellationToken cancellationToken)
    {
        var tenantId = _context.CurrentTenantId;

        // Load the current tenant's active product.
        var product = await _context.Set<SalesProductReadModel>().SingleOrDefaultAsync(
            x => x.Id == productId && x.TenantId == tenantId, cancellationToken);

        if (product == null || product.Status != Active)
            return Result<ValidatedSaleItemDto>.Failure("Product was not found or is inactive.");

        //  Product with variants cannot be sold as its parent.
        var variants = _context.Set<SalesVariantReadModel>()
            .Where(x => x.TenantId == tenantId && x.ProductId == productId);

        SalesVariantReadModel? variant = null;

        if (productVariantId.HasValue)
        {
            variant = await variants.SingleOrDefaultAsync(
                x => x.Id == productVariantId.Value, cancellationToken);
            if (variant == null || variant.Status != Active)
                return Result<ValidatedSaleItemDto>.Failure(
                    "Variant does not belong to this product or is inactive.");
        }
        else if (await variants.AnyAsync(cancellationToken))
        {
            return Result<ValidatedSaleItemDto>.Failure("Select a variant for this product.");
        }

        // Units can be tenant-owned or global; their quantity rule still applies.
        var unit = await _context.Set<SalesUnitReadModel>().SingleOrDefaultAsync(
            x => x.Id == product.UnitId && (x.TenantId == tenantId || x.TenantId == null),
            cancellationToken);
        if (unit == null)
            return Result<ValidatedSaleItemDto>.Failure("The product's measurement unit is unavailable.");
        if (!unit.IsDecimalAllowed && quantity != decimal.Truncate(quantity))
            return Result<ValidatedSaleItemDto>.Failure("This product requires a whole-number quantity.");

        // Use the assigned tax, never substitute a missing/inactive rate with zero or the default.
        var tax = await _context.Set<SalesTaxRateReadModel>().SingleOrDefaultAsync(
            x => x.Id == product.TaxRateId && x.TenantId == tenantId, cancellationToken);
        if (tax == null || !tax.IsActive)
            return Result<ValidatedSaleItemDto>.Failure("The product's tax rate is unavailable or inactive.");

        // Preserve zero prices and nullable identifiers on the selected variant.
        return Result<ValidatedSaleItemDto>.Success(new ValidatedSaleItemDto
        {
            ProductId = product.Id,
            ProductVariantId = variant?.Id,
            ProductName = product.NameEn,
            VariantName = variant?.Name,
            Sku = variant == null ? product.Sku : variant.Sku,
            Barcode = variant == null ? product.Barcode : variant.Barcode,
            UnitName = unit.Name,
            TrackInventory = product.TrackInventory,
            UnitPrice = variant == null ? product.SellingPrice : variant.SellingPrice,
            UnitCost = variant == null ? product.CostPrice : variant.CostPrice,
            TaxRate = tax.Rate
        });
    }

    public async Task<Result<List<ValidatedSaleItemDto>>> ValidateSaleItemsAsync(List<SaleItemValidationRequest> items, CancellationToken cancellationToken)
    {
        if (items == null || items.Count == 0)
            return Result<List<ValidatedSaleItemDto>>.Failure("At least one item is required.");

        var tenantId = _context.CurrentTenantId;

        // 1. Load every referenced product in one query.
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        var products = await _context.Set<SalesProductReadModel>()
            .Where(x => x.TenantId == tenantId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // 2. Load every variant that belongs to any of those products in one query
        var variantsByProductId = await _context.Set<SalesVariantReadModel>()
            .Where(v => v.TenantId == tenantId && productIds.Contains(v.ProductId))
            .GroupBy(x => x.Id)
            .ToDictionaryAsync(g=>g.Key,g=>g.ToList(),cancellationToken);

        // 3. Load every distinct unit referenced by those products in one query.
        var unitIds = products.Values.Select(p => p.UnitId).Distinct().ToList();

        var units = await _context.Set<SalesUnitReadModel>()
            .Where(x => unitIds.Contains(x.Id) && (x.TenantId == tenantId || x.TenantId == null))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // 4. Load every distinct tax rate referenced by those products in one query.
        var taxRateIds = products.Values.Select(p => p.TaxRateId).Distinct().ToList();

        var taxRates = await _context.Set<SalesTaxRateReadModel>()
            .Where(x => x.TenantId == tenantId && taxRateIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // 5. Validate each requested item against the in-memory lookups 
        var results = new List<ValidatedSaleItemDto>();

        foreach (var item in items)
        {
            if (!products.TryGetValue(item.ProductId, out var product) || product.Status != Active)
                return Result<List<ValidatedSaleItemDto>>.Failure(
                    $"Product {item.ProductId} was not found or is inactive.");

            variantsByProductId.TryGetValue(item.ProductId, out var productVariants);

            productVariants ??= new List<SalesVariantReadModel>();

            SalesVariantReadModel? variant = null;

            if (item.ProductVariantId.HasValue)
            {
                variant = productVariants.SingleOrDefault(v => v.Id == item.ProductVariantId.Value);
                if (variant == null || variant.Status != Active)
                    return Result<List<ValidatedSaleItemDto>>.Failure(
                        $"Variant does not belong to product {item.ProductId} or is inactive.");
            }
            else if (productVariants.Count > 0)
            {
                return Result<List<ValidatedSaleItemDto>>.Failure(
                    $"Select a variant for product {item.ProductId}.");
            }

            if (!units.TryGetValue(product.UnitId, out var unit))
                return Result<List<ValidatedSaleItemDto>>.Failure(
                    $"The measurement unit for product {item.ProductId} is unavailable.");

            if (!unit.IsDecimalAllowed && item.Quantity != decimal.Truncate(item.Quantity))
                return Result<List<ValidatedSaleItemDto>>.Failure(
                    $"Product {item.ProductId} requires a whole-number quantity.");

            if (!taxRates.TryGetValue(product.TaxRateId, out var tax) || !tax.IsActive)
                return Result<List<ValidatedSaleItemDto>>.Failure(
                    $"The tax rate for product {item.ProductId} is unavailable or inactive.");

            results.Add(new ValidatedSaleItemDto
            {
                ProductId = product.Id,
                ProductVariantId = variant?.Id,
                ProductName = product.NameEn,
                VariantName = variant?.Name,
                Sku = variant == null ? product.Sku : variant.Sku,
                Barcode = variant == null ? product.Barcode : variant.Barcode,
                UnitName = unit.Name,
                TrackInventory = product.TrackInventory,
                UnitPrice = variant == null ? product.SellingPrice : variant.SellingPrice,
                UnitCost = variant == null ? product.CostPrice : variant.CostPrice,
                TaxRate = tax.Rate
            });
        }

        return Result<List<ValidatedSaleItemDto>>.Success(results);
    }
}
