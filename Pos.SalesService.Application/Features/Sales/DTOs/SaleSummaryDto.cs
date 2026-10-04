namespace Pos.SalesService.Application.Features.Sales.DTOs;
public class SaleSummaryDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid TerminalId { get; set; }
    public Guid ShiftId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerNameSnapshot { get; set; }
    public string? ReceiptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal NetPaid => PaidAmount - ChangeAmount;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

