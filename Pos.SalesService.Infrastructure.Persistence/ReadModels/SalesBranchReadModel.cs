namespace Pos.SalesService.Infrastructure.Persistence.ReadModels;

public class SalesBranchReadModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; } = string.Empty;
    public string? ReceiptHeader { get; set; }
    public string Status { get; set; } = string.Empty;
}

