namespace Pos.SalesService.Application.Features.SalePayments.DTOs;

public class SalePaymentDto
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public string PaymentMethodNameSnapshot { get; set; } = string.Empty;
    public string PaymentMethodCodeSnapshot { get; set; } = string.Empty;
    public bool IsCashSnapshot { get; set; }
    public decimal Amount { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal RetainedAmount => Status == Pos.SalesService.Domain.Constants.PaymentStatus.Completed ? Amount - ChangeAmount : 0;
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid ReceivedByUserId { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

