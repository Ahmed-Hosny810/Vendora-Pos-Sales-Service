
namespace Pos.SalesService.Application.Features.CashierShifts.DTOs
{
    public class CloseCashierShiftResult
    {
        public Guid ShiftId { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ClosingCash { get; set; }
        public decimal Difference { get; set; }     // ClosingCash - ExpectedCash
        public DateTime ClosedAt { get; set; }
    }
}
