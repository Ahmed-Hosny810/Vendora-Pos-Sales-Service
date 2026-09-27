using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class SaleStatusHistory : SalesEntity
{
    public Guid SaleId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }

    public Sale Sale { get; set; } = null!;
}
