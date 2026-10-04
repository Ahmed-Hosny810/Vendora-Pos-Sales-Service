namespace Pos.SalesService.Application.Features.SalesReturns.DTOs;

public class SaleReturnItemInput
{
    public Guid OriginalSaleItemId { get; set; }
    public decimal Quantity { get; set; }
    public string StockCondition { get; set; } = string.Empty;
    public bool Restock { get; set; }
}

public class SaleReturnSummaryDto
{
    public Guid Id { get; set; }
    public Guid OriginalSaleId { get; set; }
    public Guid BranchId { get; set; }
    public string? ReturnNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public Guid ProcessedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class SaleReturnDetailsDto : SaleReturnSummaryDto
{
    public List<SaleReturnItemDto> Items { get; set; } = new();
}

public class SaleReturnItemDto
{
    public Guid Id { get; set; }
    public Guid OriginalSaleItemId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? VariantNameSnapshot { get; set; }
    public string UnitNameSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public string StockCondition { get; set; } = string.Empty;
    public bool Restock { get; set; }
}

public class ReturnableSaleItemDto
{
    public Guid OriginalSaleItemId { get; set; }
    public int ItemNumber { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string? VariantNameSnapshot { get; set; }
    public string UnitNameSnapshot { get; set; } = string.Empty;
    public bool TrackInventorySnapshot { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal RemainingRefundAmount { get; set; }
    public decimal RemainingTaxAmount { get; set; }
}

