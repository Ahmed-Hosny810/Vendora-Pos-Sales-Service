using Pos.SalesService.Application.Parameters;
namespace Pos.SalesService.Application.Features.Sales.Queries.GetSalesQuery;
public class GetSalesQueryParameter : RequestParameter<SaleOrderKey>
{
    public SaleFilter? Filter { get; set; }
}
public class SaleFilter
{
    public Guid? BranchId { get; set; }
    public Guid? TerminalId { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid? CashierUserId { get; set; }
    public Guid? CustomerId { get; set; }
    // The interval applies to sale creation time: inclusive start, exclusive end.
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtcExclusive { get; set; }
    public string? Status { get; set; }
    public string? ReceiptNumber { get; set; }
}
public enum SaleOrderKey 
{
    CreatedAt,
    CompletedAt,
    Total,
    ReceiptNumber 
}

