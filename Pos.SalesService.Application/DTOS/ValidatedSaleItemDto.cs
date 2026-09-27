namespace Pos.SalesService.Application.DTOs;

/// <summary>Trusted current Catalog values for a new sale item; no calculated sale totals.</summary>
public class ValidatedSaleItemDto
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public bool TrackInventory { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TaxRate { get; set; }
}
