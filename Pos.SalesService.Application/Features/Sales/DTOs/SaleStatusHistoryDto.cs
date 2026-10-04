namespace Pos.SalesService.Application.Features.Sales.DTOs;
public class SaleStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}

