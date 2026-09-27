using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class PaymentMethod : SalesEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsCash { get; set; }
    public bool RequiresReferenceNumber { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<SalePayment> SalePayments { get; set; } = new List<SalePayment>();
    public ICollection<RefundPayment> RefundPayments { get; set; } = new List<RefundPayment>();
}
