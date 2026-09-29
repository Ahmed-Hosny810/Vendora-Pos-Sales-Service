
namespace Pos.SalesService.Application.Features.Sales.DTOs.Calculations
{
    public class SaleCalculationResult
    {
        public bool PricesIncludeTax { get; set; }

        // Before discounts, using the input price basis.
        public decimal Subtotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal TaxTotal { get; set; }

        // Final amount payable, including tax.
        public decimal Total { get; set; }

        public List<SaleItemCalculationResult> Items { get; set; } = new();

        public List<SaleDiscountCalculationResult> Discounts { get; set; } = new();
    }

    public class SaleItemCalculationResult
    {
        public Guid SaleItemId { get; set; }

        public decimal Subtotal { get; set; }

        // Includes item discounts and this item's share of sale discounts.
        public decimal DiscountAmount { get; set; }

        // After discounts, excluding tax.
        public decimal NetAmount { get; set; }

        public decimal TaxAmount { get; set; }

        // NetAmount + TaxAmount.
        public decimal LineTotal { get; set; }
    }

    public class SaleDiscountCalculationResult
    {
        public Guid DiscountId { get; set; }

        // Actual monetary discount, using the input price basis.
        public decimal Amount { get; set; }

        public List<ItemDiscountShare> ItemShares { get; set; } = new();
    }

    public class ItemDiscountShare
    {
        public Guid SaleItemId { get; set; }

        public decimal Amount { get; set; }
    }
}
