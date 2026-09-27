using Microsoft.EntityFrameworkCore;
using Pos.SalesService.Application.DTOs;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Infrastructure.Persistence.Contexts;
using Pos.SalesService.Infrastructure.Persistence.ReadModels;

namespace Pos.SalesService.Infrastructure.Persistence.Services;

/// <inheritdoc />
public class SalesItemValidationService : ISalesItemValidationService
{
    private const string Active = "Active";
    private readonly ApplicationDbContext _context;

    public SalesItemValidationService(ApplicationDbContext context)
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
}
