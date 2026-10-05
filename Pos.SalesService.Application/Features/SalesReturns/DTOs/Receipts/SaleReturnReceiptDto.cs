namespace Pos.SalesService.Application.Features.SalesReturns.DTOs.Receipts;

public class SaleReturnReceiptDto
{
    public Guid ReturnId { get; set; }
    public string? ReturnNumber { get; set; }
    public DateTime CompletedAt { get; set; }
    public Guid OriginalSaleId { get; set; }
    public string? OriginalReceiptNumber { get; set; }
    public Guid BranchId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? CustomerNameSnapshot { get; set; }
    public string? CustomerPhoneSnapshot { get; set; }
    public string Reason { get; set; } = string.Empty;

    // The accepted amount includes tax and the original allocated discounts.
    public decimal RefundAmount { get; set; }
    public decimal TaxAmount => Items.Sum(x => x.TaxAmount);
    public decimal NetAmount => RefundAmount - TaxAmount;
    public decimal RefundedAmount => Payments.Sum(x => x.Amount);
    public decimal RemainingRefundAmount => RefundAmount - RefundedAmount;
    public bool IsFullyRefunded => RefundedAmount == RefundAmount;

    public List<SaleReturnReceiptItemDto> Items { get; set; } = new();
    // Only completed refund payments are projected into the receipt.
    public List<SaleReturnReceiptPaymentDto> Payments { get; set; } = new();
}

public class SaleReturnReceiptItemDto
{
    public int OriginalItemNumber { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? VariantNameSnapshot { get; set; }
    public string UnitNameSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string StockCondition { get; set; } = string.Empty;
    public bool Restock { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetAmount => RefundAmount - TaxAmount;
}

public class SaleReturnReceiptPaymentDto
{
    public string PaymentMethodNameSnapshot { get; set; } = string.Empty;
    public string PaymentMethodCodeSnapshot { get; set; } = string.Empty;
    public bool IsCashSnapshot { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? PaidAt { get; set; }
}

