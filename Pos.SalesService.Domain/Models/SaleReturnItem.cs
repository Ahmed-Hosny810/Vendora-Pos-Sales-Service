namespace Pos.SalesService.Domain.Models;

public class SaleReturnItem : SalesEntity
{
    public Guid ReturnId { get; set; }
    // Shared by both foreign keys so the returned item must belong to the return's original sale.
    public Guid OriginalSaleId { get; set; }
    public Guid OriginalSaleItemId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public bool Restock { get; set; }
    public string StockCondition { get; set; } = Constants.StockCondition.Sellable;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public SaleReturn Return { get; set; } = null!;
    public SaleItem OriginalSaleItem { get; set; } = null!;
}
