namespace Pos.SalesService.Application.Features.CashierShifts.DTOs;
public class CashierShiftDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid TerminalId { get; set; }
    public Guid CashierUserId { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Difference { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
