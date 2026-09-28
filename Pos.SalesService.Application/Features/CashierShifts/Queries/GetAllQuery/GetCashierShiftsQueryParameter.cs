using Pos.SalesService.Application.Parameters;
namespace Pos.SalesService.Application.Features.CashierShifts.Queries.GetAllQuery;
public class GetCashierShiftsQueryParameter : RequestParameter<CashierShiftOrderKey>
{
    public CashierShiftFilter? Filter { get; set; }
}
public class CashierShiftFilter
{
    public Guid? BranchId { get; set; }
    public Guid? TerminalId { get; set; }
    public Guid? CashierUserId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtcExclusive { get; set; }
}
public enum CashierShiftOrderKey { OpenedAt, ClosedAt, OpeningCash }
