using Pos.SalesService.Application.DTOs;
using Pos.SalesService.Application.DTOS;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Interfaces.Services;

/// <summary>Reads external Branch/Catalog data and returns expected business failures as results.</summary>
public interface ISalesValidationService
{
    /// <summary>Checks that the branch is active and belongs to the authenticated tenant.</summary>
    Task<Result> ValidateBranchAsync(Guid branchId, CancellationToken cancellationToken);

    /// <summary>Checks the active branch and its active, tenant-owned terminal.</summary>
    Task<Result> ValidateTerminalAsync(Guid branchId, Guid terminalId, CancellationToken cancellationToken);

    /// <summary>
    /// Validates product/variant ownership, active status, unit quantity rules and the assigned tax rate.
    /// Returns current Catalog information for creating a new sale item, not repricing an existing one.
    /// Non-stock-tracked items are valid. Variant prices/identifiers are used without parent fallbacks.
    /// Basic input validation (IDs, positive quantity and precision) belongs to the command validator.
    /// Authentication and infrastructure errors are allowed to propagate.
    /// </summary>
    Task<Result<ValidatedSaleItemDto>> ValidateSaleItemAsync(Guid productId, Guid? productVariantId,
        decimal quantity, CancellationToken cancellationToken);

    // batch version.
    Task<Result<List<ValidatedSaleItemDto>>> ValidateSaleItemsAsync(
        List<SaleItemValidationRequest> items, CancellationToken cancellationToken);
}
