
namespace Pos.SalesService.Domain.Models;

public class SaleReturn : SalesEntity
{
    public Guid BranchId { get; set; }
    public Guid OriginalSaleId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string? ReturnNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public Guid ProcessedByUserId { get; set; }
    public DateTime CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Sale OriginalSale { get; set; } = null!;
    public ICollection<SaleReturnItem> Items { get; set; } = new List<SaleReturnItem>();
    public ICollection<RefundPayment> RefundPayments { get; set; } = new List<RefundPayment>();
}
