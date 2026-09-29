
namespace Pos.SalesService.Application.Features.Sales.DTOs.Calculations
{
    public class SaleCalculationInput
    {
        public bool PricesIncludeTax { get; set; }

        public List<SaleItemCalculationInput> Items { get; set; } = new();

        public List<SaleDiscountCalculationInput> Discounts { get; set; } = new();
    }

    public class SaleItemCalculationInput
    {
        public Guid SaleItemId { get; set; }

        public int ItemNumber { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        // Percentage: 14 means 14%.
        public decimal TaxRate { get; set; }
    }

    public class SaleDiscountCalculationInput
    {
        public Guid DiscountId { get; set; }

        // Null applies to the whole sale.
        public Guid? SaleItemId { get; set; }

        // Use  existing DiscountType string constants.
        public string DiscountType { get; set; } = string.Empty;

        // Money for FixedAmount; percentage for Percentage.
        public decimal Value { get; set; }

        // Defines a stable order when several discounts are applied.
        public int ApplicationOrder { get; set; }
    }
}
