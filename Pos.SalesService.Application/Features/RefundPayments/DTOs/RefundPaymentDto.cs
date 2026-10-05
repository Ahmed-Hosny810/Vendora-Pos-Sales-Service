namespace Pos.SalesService.Application.Features.RefundPayments.DTOs;

public class RefundPaymentDto
{
    public Guid Id { get; set; }
    public Guid ReturnId { get; set; }
    public Guid ShiftId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string PaymentMethodNameSnapshot { get; set; } = string.Empty;
    public string PaymentMethodCodeSnapshot { get; set; } = string.Empty;
    public bool IsCashSnapshot { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid PaidByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

