using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class SalePayment : SalesEntity
{
    public Guid SaleId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public string PaymentMethodNameSnapshot { get; set; } = string.Empty;
    public string PaymentMethodCodeSnapshot { get; set; } = string.Empty;
    public bool IsCashSnapshot { get; set; }
    // Amount is money tendered; Amount - ChangeAmount is retained for the sale.
    public decimal Amount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = PaymentStatus.Pending;
    public Guid ReceivedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Sale Sale { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
}
