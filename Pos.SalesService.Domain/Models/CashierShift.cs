using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class CashierShift : SalesEntity
{
    public Guid BranchId { get; set; }
    public Guid TerminalId { get; set; }
    public Guid CashierUserId { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Difference { get; set; }
    public string Status { get; set; } = CashierShiftStatus.Open;
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<RefundPayment> RefundPayments { get; set; } = new List<RefundPayment>();
}
