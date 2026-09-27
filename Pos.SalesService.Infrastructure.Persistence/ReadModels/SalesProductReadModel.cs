namespace Pos.SalesService.Infrastructure.Persistence.ReadModels;

public class SalesProductReadModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UnitId { get; set; }
    public Guid TaxRateId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool TrackInventory { get; set; }
}
