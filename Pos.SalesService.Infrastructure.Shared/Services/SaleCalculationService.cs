using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Helpers;
using Pos.SalesService.Application.Interfaces.Services;
using Pos.SalesService.Application.Wrappers;
using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Infrastructure.Shared.Services
{
    public class SaleCalculationService : ISaleCalculationService
    {
        public Result<SaleCalculationResult> CalculateSale(SaleCalculationInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // 1. Validate the input.
            if (input.Items == null || input.Items.Count == 0)
                return Result<SaleCalculationResult>.Failure(
                    "The sale must contain at least one item.");

            if (input.Discounts == null)
                return Result<SaleCalculationResult>.Failure(
                    "The discounts collection is required.");

            var itemIds = new HashSet<Guid>();

            foreach (var item in input.Items)
            {
                if (item == null ||
                    item.SaleItemId == Guid.Empty ||
                    !itemIds.Add(item.SaleItemId))
                {
                    return Result<SaleCalculationResult>.Failure(
                        "Items must have unique, non-empty IDs.");
                }

                ValidateItem(item);
            }

            var discountIds = new HashSet<Guid>();

            foreach (var discount in input.Discounts)
            {
                if (discount == null ||
                    discount.DiscountId == Guid.Empty ||
                    !discountIds.Add(discount.DiscountId))
                {
                    return Result<SaleCalculationResult>.Failure(
                        "Discounts must have unique, non-empty IDs.");
                }

                if (discount.SaleItemId.HasValue &&
                    !itemIds.Contains(discount.SaleItemId.Value))
                {
                    return Result<SaleCalculationResult>.Failure(
                        "A discount references an item outside this sale.");
                }

                if (discount.DiscountType != DiscountType.FixedAmount &&
                    discount.DiscountType != DiscountType.Percentage)
                {
                    return Result<SaleCalculationResult>.Failure(
                        "Invalid discount type.");
                }

                if (discount.Value <= 0 ||
                    decimal.Round(discount.Value, 2) != discount.Value ||
                    (discount.DiscountType == DiscountType.Percentage &&
                     discount.Value > 100))
                {
                    return Result<SaleCalculationResult>.Failure(
                        "Invalid discount value.");
                }
            }

            try
            {
                // 2. Calculate item subtotals.
                var subtotalByItemId = input.Items.ToDictionary(i => i.SaleItemId,i => CurrencyRounding.Round(i.Quantity * i.UnitPrice));

                var currentAmountByItemId = new Dictionary<Guid, decimal>(subtotalByItemId);

                var itemsSharesByDiscountId = new Dictionary<Guid, List<ItemDiscountShare>>();

                if (input.Discounts.Count > 0)
                {
                    // 3. Apply item-level discounts.
                    var itemDiscountResult = ApplyItemDiscounts(
                    input.Discounts,
                    currentAmountByItemId,
                    itemsSharesByDiscountId);

                    if (itemDiscountResult.IsFailure)
                        return Result<SaleCalculationResult>.Failure(itemDiscountResult.Errors.ToArray());

                    // 4. Allocate and apply whole-sale discounts.
                    var saleDiscountResult = ApplySaleDiscounts(
                        input.Items,
                        input.Discounts,
                        currentAmountByItemId,
                        itemsSharesByDiscountId);

                    if (saleDiscountResult.IsFailure)
                        return Result<SaleCalculationResult>.Failure(
                            saleDiscountResult.Errors.ToArray());

                }

                // 5. Calculate tax and final item amounts.
                var itemResults = CalculateItemTaxes(
                    input.Items,
                    input.PricesIncludeTax,
                    subtotalByItemId,
                    currentAmountByItemId);

                // 6. Build discount details.
                var discountResults = itemsSharesByDiscountId
                    .Select(kv => new SaleDiscountCalculationResult
                    {
                        DiscountId = kv.Key,
                        Amount = kv.Value.Sum(a => a.Amount),
                        ItemShares = kv.Value
                            .Select(a => new ItemDiscountShare
                            {
                                SaleItemId = a.SaleItemId,
                                Amount = a.Amount
                            })
                            .ToList()
                    })
                    .ToList();

                // 7. Sum final item values so sale totals match the items.
                var result = new SaleCalculationResult
                {
                    PricesIncludeTax = input.PricesIncludeTax,
                    Subtotal = itemResults.Sum(i => i.Subtotal),
                    DiscountTotal = itemResults.Sum(i => i.DiscountAmount),
                    TaxTotal = itemResults.Sum(i => i.TaxAmount),
                    Total = itemResults.Sum(i => i.LineTotal),
                    Items = itemResults,
                    Discounts = discountResults
                };

                return Result<SaleCalculationResult>.Success(result);
            }
            catch (OverflowException)
            {
                return Result<SaleCalculationResult>.Failure("The supplied values exceed the supported calculation range.");
            }
        }
        public Result<PaymentCalculationResult> CalculatePayments(PaymentCalculationInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // 1. Validate amounts and prevent counting the same payment twice.
            if (!IsValidMoney(input.SaleTotal) || input.Payments == null)
                return Result<PaymentCalculationResult>.Failure("A valid sale total and payments collection are required.");

            var paymentIds = new HashSet<Guid>();

            foreach (var payment in input.Payments)
            {
                if (payment == null || payment.PaymentId == Guid.Empty ||
                    !paymentIds.Add(payment.PaymentId))
                    return Result<PaymentCalculationResult>.Failure("Payments must have unique, non-empty IDs.");

                if (payment.Status != PaymentStatus.Pending &&
                    payment.Status != PaymentStatus.Completed &&
                    payment.Status != PaymentStatus.Failed &&
                    payment.Status != PaymentStatus.Cancelled)
                    return Result<PaymentCalculationResult>.Failure("Invalid payment status.");

                if (!IsValidMoney(payment.Amount) || payment.Amount == 0 ||
                    !IsValidMoney(payment.ChangeAmount) ||
                    payment.ChangeAmount > payment.Amount)
                    return Result<PaymentCalculationResult>.Failure("Invalid payment amount or change.");

                if (!payment.IsCash && payment.ChangeAmount != 0)
                    return Result<PaymentCalculationResult>.Failure("Only cash payments can include change.");
            }

            try
            {
                // 2. Only completed payments contribute to settlement.
                var completedPayments = input.Payments.Where(p => p.Status == PaymentStatus.Completed).ToList();
                var paidAmount = completedPayments.Sum(p => p.Amount);
                var changeAmount = completedPayments.Sum(p => p.ChangeAmount);
                var netPaid = paidAmount - changeAmount;

                if (!IsValidMoney(paidAmount) || !IsValidMoney(changeAmount))
                    return Result<PaymentCalculationResult>.Failure("Payment totals exceed the supported range.");

                if (netPaid > input.SaleTotal)
                    return Result<PaymentCalculationResult>.Failure(
                        "Net payment exceeds the sale total. Check the recorded payment and change amounts.");

                // 3. Return totals without changing payments or their statuses.
                return Result<PaymentCalculationResult>.Success(new PaymentCalculationResult
                {
                    PaidAmount = paidAmount,
                    ChangeAmount = changeAmount,
                    NetPaid = netPaid,
                    RemainingDue = Math.Max(input.SaleTotal - netPaid, 0),
                    IsFullyPaid = netPaid == input.SaleTotal
                });
            }
            catch (OverflowException)
            {
                return Result<PaymentCalculationResult>.Failure("Payment totals exceed the supported calculation range.");
            }
        }

        public Result<ReturnCalculationResult> CalculateReturn(ReturnCalculationInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // 1. Validate original amounts and previous return allocations.
            if (input.Items == null || input.Items.Count == 0)
                return Result<ReturnCalculationResult>.Failure("The return must contain at least one item.");

            var returnItemIds = new HashSet<Guid>();

            foreach (var item in input.Items)
            {
                if (item == null || item.ReturnItemId == Guid.Empty ||
                    item.OriginalSaleItemId == Guid.Empty || !returnItemIds.Add(item.ReturnItemId))
                    return Result<ReturnCalculationResult>.Failure("Return items must have unique, non-empty IDs and an original sale item.");

                var validation = ValidateReturnItem(item);

                if (validation.IsFailure)
                    return Result<ReturnCalculationResult>.Failure(validation.Errors.ToArray());
            }

            try
            {
                var itemResults = new List<ReturnItemCalculationResult>();

                // 2. Calculate the remaining quantity and refundable amounts for each item.
                foreach (var group in input.Items.GroupBy(x => x.OriginalSaleItemId))
                {
                    var original = group.First();
                    if (group.Any(x => x.OriginalQuantity != original.OriginalQuantity ||
                        x.OriginalLineTotal != original.OriginalLineTotal ||
                        x.OriginalTaxAmount != original.OriginalTaxAmount ||
                        x.PreviouslyReturnedQuantity != original.PreviouslyReturnedQuantity ||
                        x.PreviouslyReturnedAmount != original.PreviouslyReturnedAmount ||
                        x.PreviouslyReturnedTaxAmount != original.PreviouslyReturnedTaxAmount))
                        return Result<ReturnCalculationResult>.Failure("Entries for one original item must use the same saved amounts.");

                    if (group.Sum(x => x.QuantityToReturn) > original.OriginalQuantity - original.PreviouslyReturnedQuantity)
                        return Result<ReturnCalculationResult>.Failure("The requested quantity exceeds the remaining returnable quantity.");

                    var allocatedQuantity = original.PreviouslyReturnedQuantity;
                    var allocatedAmount = original.PreviouslyReturnedAmount;
                    var allocatedTax = original.PreviouslyReturnedTaxAmount;
                    // The caller supplies canonical condition/restock order.
                    foreach (var item in group)
                    {
                        var quantityAvailableToReturn = item.OriginalQuantity - allocatedQuantity;

                        if (item.QuantityToReturn > quantityAvailableToReturn)
                            return Result<ReturnCalculationResult>.Failure("The requested quantity exceeds the remaining returnable quantity.");

                        var originalAmountExcludingTax = item.OriginalLineTotal - item.OriginalTaxAmount;

                        var netAmountAvailableToRefund = originalAmountExcludingTax - (allocatedAmount - allocatedTax);

                        var taxAmountAvailableToRefund = item.OriginalTaxAmount - allocatedTax;

                        // 3. Refund the proportional amounts, or all remaining amounts for the final quantity.
                        var returnsAllRemainingQuantity = item.QuantityToReturn == quantityAvailableToReturn;
                        var returnedShareOfOriginalQuantity = item.QuantityToReturn / item.OriginalQuantity;

                        var netRefundAmount = returnsAllRemainingQuantity
                            ? netAmountAvailableToRefund
                            : Math.Min(CurrencyRounding.Round(originalAmountExcludingTax * returnedShareOfOriginalQuantity), netAmountAvailableToRefund);

                        var taxRefundAmount = returnsAllRemainingQuantity
                            ? taxAmountAvailableToRefund
                            : Math.Min(CurrencyRounding.Round(item.OriginalTaxAmount * returnedShareOfOriginalQuantity), taxAmountAvailableToRefund);

                        itemResults.Add(new ReturnItemCalculationResult
                        {
                            ReturnItemId = item.ReturnItemId,
                            OriginalSaleItemId = item.OriginalSaleItemId,
                            Quantity = item.QuantityToReturn,
                            NetAmount = netRefundAmount,
                            TaxAmount = taxRefundAmount,
                            RefundAmount = netRefundAmount + taxRefundAmount
                        });
                        allocatedQuantity += item.QuantityToReturn;
                        allocatedAmount += netRefundAmount + taxRefundAmount;
                        allocatedTax += taxRefundAmount;
                    }
                }
                // 4. Sum the calculated items so all return totals reconcile.
                var result = new ReturnCalculationResult
                {
                    NetAmount = itemResults.Sum(i => i.NetAmount),
                    TaxAmount = itemResults.Sum(i => i.TaxAmount),
                    RefundAmount = itemResults.Sum(i => i.RefundAmount),
                    Items = itemResults
                };

                if (!IsValidMoney(result.RefundAmount))
                    return Result<ReturnCalculationResult>.Failure("The return total exceeds the supported range.");

                return Result<ReturnCalculationResult>.Success(result);
            }
            catch (OverflowException)
            {
                return Result<ReturnCalculationResult>.Failure("The supplied values exceed the supported calculation range.");
            }
        }

        private static Result ValidateReturnItem(ReturnItemCalculationInput item)
        {
            if (!IsValidQuantity(item.OriginalQuantity) || item.OriginalQuantity == 0 ||
                !IsValidQuantity(item.QuantityToReturn) || item.QuantityToReturn == 0 ||
                !IsValidQuantity(item.PreviouslyReturnedQuantity) ||
                item.PreviouslyReturnedQuantity > item.OriginalQuantity)
                return Result.Failure("Invalid original, previous or requested return quantity.");

            if (!IsValidMoney(item.OriginalLineTotal) ||
                !IsValidMoney(item.OriginalTaxAmount) ||
                !IsValidMoney(item.PreviouslyReturnedAmount) ||
                !IsValidMoney(item.PreviouslyReturnedTaxAmount))
                return Result.Failure("Return amounts must be non-negative, within range and have at most two decimal places.");

            if (item.OriginalTaxAmount > item.OriginalLineTotal ||
                item.PreviouslyReturnedAmount > item.OriginalLineTotal ||
                item.PreviouslyReturnedTaxAmount > item.OriginalTaxAmount ||
                item.PreviouslyReturnedTaxAmount > item.PreviouslyReturnedAmount ||
                item.PreviouslyReturnedAmount - item.PreviouslyReturnedTaxAmount >
                    item.OriginalLineTotal - item.OriginalTaxAmount)
                return Result.Failure("Previous return amounts exceed the original net or tax amounts.");

            if (item.PreviouslyReturnedQuantity == 0 &&
                (item.PreviouslyReturnedAmount != 0 || item.PreviouslyReturnedTaxAmount != 0))
                return Result.Failure("Previous return amounts require a previous returned quantity.");

            return Result.Success();
        }


        //Helpers
        private static bool IsValidMoney(decimal amount)
        {
            return amount >= 0 && amount <= 9999999999999999.99m &&
                CurrencyRounding.Round(amount) == amount;
        }

        private static bool IsValidQuantity(decimal quantity)
        {
            return quantity >= 0 && quantity <= 999999999999999.999m &&
                decimal.Round(quantity, 3) == quantity;
        }

        private Result ApplyItemDiscounts(
            IEnumerable<SaleDiscountCalculationInput> discounts,
            Dictionary<Guid, decimal> currentAmountByItemId,
            Dictionary<Guid, List<ItemDiscountShare>> itemsSharesByDiscountId)
        {
            foreach (var discount in discounts.Where(d => d.SaleItemId.HasValue).OrderBy(d => d.ApplicationOrder))
            {
                var itemId = discount.SaleItemId!.Value;

                var currentAmount = currentAmountByItemId[itemId];

                var discountAmount = CurrencyRounding.Round(
                    ComputeDiscountAmount(discount, currentAmount));

                if (discountAmount > currentAmount)
                {
                    return Result.Failure(
                        "The item discount exceeds the item's remaining amount.");
                }

                currentAmountByItemId[itemId] = currentAmount - discountAmount;

                RecordItemDiscount(
                    itemsSharesByDiscountId,
                    discount.DiscountId,
                    itemId,
                    discountAmount);
            }

            return Result.Success();
        }

        private Result ApplySaleDiscounts(
            IEnumerable<SaleItemCalculationInput> items,
            IEnumerable<SaleDiscountCalculationInput> discounts,
            Dictionary<Guid, decimal> currentAmountByItemId,
            Dictionary<Guid, List<ItemDiscountShare>> itemsSharesByDiscountId)
        {
            foreach (var discount in discounts.Where(d => !d.SaleItemId.HasValue).OrderBy(d => d.ApplicationOrder))
            {
                var totalCurrentAmount = currentAmountByItemId.Values.Sum();

                var discountAmount = CurrencyRounding.Round(
                    ComputeDiscountAmount(discount, totalCurrentAmount));

                if (discountAmount > totalCurrentAmount)
                {
                    return Result.Failure(
                        "The sale discount exceeds the sale's remaining amount.");
                }

                // Preserve a result even when the calculated discount is zero.
                itemsSharesByDiscountId[discount.DiscountId] =
                    new List<ItemDiscountShare>();

                if (discountAmount == 0)
                    continue;

                // Calculate proportional shares in cents.
                var discountCents = discountAmount * 100m;

                var itemShares = items
                    .Where(i => currentAmountByItemId[i.SaleItemId] > 0)
                    .Select(item =>
                    {
                        var currentAmount =
                            currentAmountByItemId[item.SaleItemId];

                        var exactCents =
                            (currentAmount / totalCurrentAmount) * discountCents;

                        var wholeCents = decimal.Floor(exactCents);

                        return new
                        {
                            item.SaleItemId,
                            item.ItemNumber,
                            WholeCents = wholeCents,
                            Remainder = exactCents - wholeCents
                        };
                    })
                    .ToList();

                var discountCentsByItemId = itemShares.ToDictionary(
                    i => i.SaleItemId,
                    i => i.WholeCents);

                var centsLeft =
                    discountCents - itemShares.Sum(i => i.WholeCents);

                // Assign leftover cents deterministically.
                foreach (var itemShare in itemShares.OrderByDescending(i => i.Remainder).ThenBy(i => i.ItemNumber).ThenBy(i => i.SaleItemId))
                {
                    if (centsLeft == 0)
                        break;

                    discountCentsByItemId[itemShare.SaleItemId] += 1m;
                    centsLeft -= 1m;
                }

                // Apply and record each item's share.
                foreach (var itemShare in itemShares)
                {
                    var itemId = itemShare.SaleItemId;
                    var currentAmount = currentAmountByItemId[itemId];

                    var itemDiscountAmount =
                        discountCentsByItemId[itemId] / 100m;

                    currentAmountByItemId[itemId] =
                        currentAmount - itemDiscountAmount;

                    RecordItemDiscount(
                        itemsSharesByDiscountId,
                        discount.DiscountId,
                        itemId,
                        itemDiscountAmount);
                }
            }

            return Result.Success();
        }

        private List<SaleItemCalculationResult> CalculateItemTaxes(
             IEnumerable<SaleItemCalculationInput> items,
             bool pricesIncludeTax,
             IReadOnlyDictionary<Guid, decimal> subtotalByItemId,
             IReadOnlyDictionary<Guid, decimal> currentAmountByItemId)
        {
            var itemResults = new List<SaleItemCalculationResult>();

            foreach (var item in items)
            {
                var subtotal = subtotalByItemId[item.SaleItemId];
                var currentAmount = currentAmountByItemId[item.SaleItemId];
                var discountAmount = subtotal - currentAmount;

                decimal netAmount;
                decimal taxAmount;
                decimal lineTotal;

                if (pricesIncludeTax)
                {
                    lineTotal = currentAmount;

                    taxAmount = CurrencyRounding.Round(
                        currentAmount * (item.TaxRate / (100m + item.TaxRate)));

                    netAmount = lineTotal - taxAmount;
                }
                else
                {
                    netAmount = currentAmount;

                    taxAmount = CurrencyRounding.Round(
                        netAmount * (item.TaxRate / 100m));

                    lineTotal = netAmount + taxAmount;
                }

                itemResults.Add(new SaleItemCalculationResult
                {
                    SaleItemId = item.SaleItemId,
                    Subtotal = subtotal,
                    DiscountAmount = discountAmount,
                    NetAmount = netAmount,
                    TaxAmount = taxAmount,
                    LineTotal = lineTotal
                });
            }

            return itemResults;
        }


        private static decimal ComputeDiscountAmount(SaleDiscountCalculationInput discount, decimal baseAmount)
        {

           return  discount.DiscountType == DiscountType.Percentage? baseAmount * (discount.Value / 100) : discount.Value;
        }

        private static void RecordItemDiscount(
            Dictionary<Guid, List<ItemDiscountShare>> discountSharesByDiscountId,
            Guid discountId, Guid saleItemId, decimal amount)
        {
            if (!discountSharesByDiscountId.TryGetValue(discountId, out var itemShares))
            {
                itemShares = new List<ItemDiscountShare>();
                discountSharesByDiscountId[discountId] = itemShares;
            }
            itemShares.Add(new ItemDiscountShare { SaleItemId = saleItemId, Amount = amount });
        }

        private static void ValidateItem(SaleItemCalculationInput item)
        {
            if (item.Quantity < 0)
                throw new ArgumentException($"Item {item.SaleItemId}: quantity must be positive.");

            if (item.UnitPrice < 0)
                throw new ArgumentException($"Item {item.SaleItemId}: unit price must be positive.");

            if (item.TaxRate < 0)
                throw new ArgumentException($"Item {item.SaleItemId}: tax rate must be positive.");

            if(item.TaxRate < 0 || item.TaxRate > 100)
                throw new ArgumentException($"Item {item.SaleItemId}: tax rate {item.TaxRate} is out of range.");

        }
    }
}
