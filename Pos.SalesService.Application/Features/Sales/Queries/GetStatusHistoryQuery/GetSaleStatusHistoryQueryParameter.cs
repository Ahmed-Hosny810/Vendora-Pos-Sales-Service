using Pos.SalesService.Application.Parameters;
namespace Pos.SalesService.Application.Features.Sales.Queries.GetStatusHistoryQuery;
public class GetSaleStatusHistoryQueryParameter : RequestParameter<SaleStatusHistoryOrderKey>
{
    public SaleStatusHistoryFilter? Filter { get; set; }
}
public class SaleStatusHistoryFilter
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtcExclusive { get; set; }
    public string? NewStatus { get; set; }
    public Guid? ChangedByUserId { get; set; }
}
public enum SaleStatusHistoryOrderKey { ChangedAt }

