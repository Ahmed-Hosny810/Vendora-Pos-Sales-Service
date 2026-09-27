using Pos.SalesService.Domain.Constants;

namespace Pos.SalesService.Domain.Models;

public class RefundPayment : SalesEntity
{
    public Guid ReturnId { get; set; }
    // The paying shift may differ from the shift that made the original sale.
    public Guid ShiftId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public string PaymentMethodNameSnapshot { get; set; } = string.Empty;
    public string PaymentMethodCodeSnapshot { get; set; } = string.Empty;
    public bool IsCashSnapshot { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = PaymentStatus.Pending;
    public Guid PaidByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public SaleReturn Return { get; set; } = null!;
    public CashierShift Shift { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = null!;
}
