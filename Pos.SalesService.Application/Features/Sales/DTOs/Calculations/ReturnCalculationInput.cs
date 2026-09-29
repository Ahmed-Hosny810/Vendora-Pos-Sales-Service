
namespace Pos.SalesService.Application.Features.Sales.DTOs.Calculations
{
    public class ReturnCalculationInput
    {
        public List<ReturnItemCalculationInput> Items { get; set; } = new();
    }

    public class ReturnItemCalculationInput
    {
        // Identifies this particular return item.
        public Guid ReturnItemId { get; set; }

        public Guid OriginalSaleItemId { get; set; }

        public decimal OriginalQuantity { get; set; }

        // Original final item total, including tax and after all discounts.
        public decimal OriginalLineTotal { get; set; }

        public decimal OriginalTaxAmount { get; set; }

        public decimal PreviouslyReturnedQuantity { get; set; }

        // Previously allocated portions of OriginalLineTotal and OriginalTaxAmount.
        public decimal PreviouslyReturnedAmount { get; set; }

        public decimal PreviouslyReturnedTaxAmount { get; set; }

        public decimal QuantityToReturn { get; set; }
    }

    public class ReturnCalculationResult
    {
        // Refund excluding tax.
        public decimal NetAmount { get; set; }

        public decimal TaxAmount { get; set; }

        // NetAmount + TaxAmount.
        public decimal RefundAmount { get; set; }

        public List<ReturnItemCalculationResult> Items { get; set; } = new();
    }

    public class ReturnItemCalculationResult
    {
        public Guid ReturnItemId { get; set; }

        public Guid OriginalSaleItemId { get; set; }

        public decimal Quantity { get; set; }

        public decimal NetAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal RefundAmount { get; set; }
    }
}
