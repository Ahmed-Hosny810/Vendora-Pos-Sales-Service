
namespace Pos.SalesService.Application.Features.Sales.DTOs.Receipts
{
    public class SaleReceiptDto
    {
        public Guid SaleId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }

        public Guid BranchId { get; set; }
        public string? BranchNameAr { get; set; } = string.Empty;
        public string BranchNameEn { get; set; } = string.Empty;
        public string? ReceiptHeader { get; set; }   // from Branches.ReceiptHeader
        public string? ReceiptFooterAr { get; set; }
        public string? ReceiptFooterEn { get; set; }   // from TenantSettings.ReceiptFooter

        public string? CustomerNameSnapshot { get; set; }
        public string? CustomerPhoneSnapshot { get; set; }

        public List<SaleReceiptItemDto> Items { get; set; } = new();
        public List<SaleReceiptPaymentDto> Payments { get; set; } = new();

        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
    }

    public class SaleReceiptItemDto
    {
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public string? VariantNameSnapshot { get; set; }
        public decimal Quantity { get; set; }
        public string UnitNameSnapshot { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class SaleReceiptPaymentDto
    {
        public string PaymentMethodNameSnapshot { get; set; } = string.Empty;
        public bool IsCashSnapshot { get; set; }
        public decimal Amount { get; set; }
        public decimal ChangeAmount { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
