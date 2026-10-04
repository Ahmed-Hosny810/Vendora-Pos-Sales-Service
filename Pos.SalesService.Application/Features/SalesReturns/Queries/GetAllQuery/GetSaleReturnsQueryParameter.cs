using Pos.SalesService.Application.Parameters;

namespace Pos.SalesService.Application.Features.SalesReturns.Queries.GetAllQuery;

public class GetSaleReturnsQueryParameter : RequestParameter<SaleReturnOrderKey>
{
    public SaleReturnFilter? Filter { get; set; }
}

public class SaleReturnFilter
{
    public Guid? OriginalSaleId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public string? Status { get; set; }
    public string? ReturnNumber { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtcExclusive { get; set; }
}

public enum SaleReturnOrderKey
{
    CreatedAt,
    RefundAmount,
    ReturnNumber
}

