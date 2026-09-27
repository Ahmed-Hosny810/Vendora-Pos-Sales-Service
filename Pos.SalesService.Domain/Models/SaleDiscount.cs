using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class SaleDiscount : SalesEntity
{
    public Guid SaleId { get; set; }
    // Null applies to the whole sale; otherwise this is an item discount.
    public Guid? SaleItemId { get; set; }
    public string DiscountType { get; set; } = Constants.DiscountType.FixedAmount;
    public decimal Value { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Sale Sale { get; set; } = null!;
    public SaleItem? SaleItem { get; set; }
}
