using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Features.SalesReturns.DTOs;
using Pos.SalesService.Application.Interfaces.Repositories;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;
using Pos.SalesService.Domain.Models;

namespace Pos.SalesService.Application.Features.SalesReturns.Services;

public class SaleReturnService
{
    private readonly ISaleReturnRepositoryAsync _returnRepository;
    private readonly ISaleCalculationService _calculator;

    public SaleReturnService(ISaleReturnRepositoryAsync repository, ISaleCalculationService calculator)
    {
        _returnRepository = repository;
        _calculator = calculator;
    }

    public async Task<Result<SaleReturn>> PrepareAsync(Guid tenantId, Guid saleId, Guid returnId, IReadOnlyList<SaleReturnItemInput> inputs, CancellationToken cancellationToken)
    {
        if (inputs == null || inputs.Count == 0 || inputs.Any(x => x == null))
            return Result<SaleReturn>.Failure("At least one valid return item is required.");

        var sale = await _returnRepository.GetOriginalSaleAsync(tenantId, saleId, cancellationToken);

        if (sale == null)
            return Result<SaleReturn>.Failure("The original sale was not found.");

        if (sale.Status != SaleStatus.Completed && sale.Status != SaleStatus.PartiallyReturned)
            return Result<SaleReturn>.Failure("Only completed or partially returned sales can be returned.");

        // Reject duplicate ,each original item can only be returned once per request.
        var duplicateIds = inputs
            .GroupBy(x => x.OriginalSaleItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
            return Result<SaleReturn>.Failure("Each original sale item can appear only once in a return request.");

        var originalItems = sale.Items.ToDictionary(x => x.Id);

        var returnCalculationInput = new ReturnCalculationInput();

        var prepared = new SaleReturn
        {
            Id = returnId,
            TenantId = tenantId,
            OriginalSaleId = sale.Id,
            BranchId = sale.BranchId,
            OriginalSale = sale
        };

        // Ordering by the original item's position on the sale (ItemNumber)
        foreach (var input in inputs.OrderBy(x => originalItems.TryGetValue(x.OriginalSaleItemId, out var item) ? item.ItemNumber : int.MaxValue)
                     .ThenBy(x => x.StockCondition, StringComparer.Ordinal).ThenBy(x => x.Restock))
        {
            if (!originalItems.TryGetValue(input.OriginalSaleItemId, out var originalItem))
                return Result<SaleReturn>.Failure("Every returned item must belong to the original sale.");

            if (input.StockCondition != StockCondition.Sellable &&
                input.StockCondition != StockCondition.Damaged &&
                input.StockCondition != StockCondition.Expired)
                return Result<SaleReturn>.Failure("Invalid stock condition.");

            if (input.Restock && (!originalItem.TrackInventorySnapshot || input.StockCondition != StockCondition.Sellable))
                return Result<SaleReturn>.Failure("Only sellable inventory-tracked items can be restocked.");

            var item = new SaleReturnItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ReturnId = returnId,
                OriginalSaleId = sale.Id,
                OriginalSaleItemId = originalItem.Id,
                ProductId = originalItem.ProductId,
                ProductVariantId = originalItem.ProductVariantId,
                Quantity = input.Quantity,
                StockCondition = input.StockCondition,
                Restock = input.Restock
            };

            prepared.Items.Add(item);

            returnCalculationInput.Items.Add(new ReturnItemCalculationInput
            {
                ReturnItemId = item.Id,
                OriginalSaleItemId = originalItem.Id,
                OriginalQuantity = originalItem.Quantity,
                OriginalLineTotal = originalItem.LineTotal,
                OriginalTaxAmount = originalItem.TaxAmount,
                PreviouslyReturnedQuantity = originalItem.ReturnedQuantity,
                PreviouslyReturnedAmount = originalItem.ReturnItems.Sum(x => x.RefundAmount),
                PreviouslyReturnedTaxAmount = originalItem.ReturnItems.Sum(x => x.TaxAmount),
                QuantityToReturn = input.Quantity
            });
        }

        var result = _calculator.CalculateReturn(returnCalculationInput);

        if (result.IsFailure)
            return Result<SaleReturn>.Failure(result.Errors.ToArray());

        var amounts = result.Value!.Items.ToDictionary(x => x.ReturnItemId);

        foreach (var item in prepared.Items)
        {
            item.RefundAmount = amounts[item.Id].RefundAmount;
            item.TaxAmount = amounts[item.Id].TaxAmount;
        }

        prepared.RefundAmount = result.Value.RefundAmount;

        return Result<SaleReturn>.Success(prepared);
    }
}
