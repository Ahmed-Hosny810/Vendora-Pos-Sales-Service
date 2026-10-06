
namespace Pos.SalesService.Application.Features.CashierShifts.DTOs
{
    public class CashierShiftSummaryDto
    {
        public Guid ShiftId { get; set; }
        public Guid CashierUserId { get; set; }
        public Guid BranchId { get; set; }
        public Guid TerminalId { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal CashReceipts { get; set; }
        public decimal CashRefunds { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal NonCashReceipts { get; set; }

        public decimal TotalReceipts => CashReceipts + NonCashReceipts;
        public decimal? ExpectedCash { get; set; }
        public decimal? ClosingCash { get; set; }
        public decimal? Difference { get; set; }
    }
}
