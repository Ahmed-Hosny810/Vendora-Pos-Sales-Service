namespace Pos.SalesService.Infrastructure.Persistence.ReadModels;

public class SalesVariantReadModel
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}
