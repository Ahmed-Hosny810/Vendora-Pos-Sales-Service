namespace Pos.SalesService.Application.Features.Sales.DTOs
{
    public class SaleDetailsDto
    {
        public Guid Id { get; set; }
        public Guid BranchId { get; set; }
        public Guid TerminalId { get; set; }
        public Guid ShiftId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerNameSnapshot { get; set; }
        public string? CustomerPhoneSnapshot { get; set; }
        public string? DeliveryAddressSnapshot { get; set; }
        public string? ReceiptNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public bool PricesIncludeTax { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal NetPaid => PaidAmount - ChangeAmount;
        public decimal RemainingDue => Math.Max(Total - NetPaid, 0);
        public bool IsFullyPaid => NetPaid == Total;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<SaleItemDetailsDto> Items { get; set; } = new();
    }

    public class SaleItemDetailsDto
    {
        public Guid Id { get; set; }
        public int ItemNumber { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public string? VariantNameSnapshot { get; set; }
        public string? SkuSnapshot { get; set; }
        public string? BarcodeSnapshot { get; set; }
        public string UnitNameSnapshot { get; set; } = string.Empty;
        public bool TrackInventorySnapshot { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal LineTotal { get; set; }
        public decimal ReturnedQuantity { get; set; }
    }
}