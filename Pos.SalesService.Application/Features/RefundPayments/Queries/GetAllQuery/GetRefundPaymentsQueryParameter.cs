using Pos.SalesService.Application.Parameters;

namespace Pos.SalesService.Application.Features.RefundPayments.Queries.GetAllQuery;

public class GetRefundPaymentsQueryParameter : RequestParameter<RefundPaymentOrderKey>
{
    public RefundPaymentFilter? Filter { get; set; }
}

public class RefundPaymentFilter
{
    public Guid? PaymentMethodId { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid? PaidByUserId { get; set; }
    public bool? IsCash { get; set; }
    public string? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtcExclusive { get; set; }
}

public enum RefundPaymentOrderKey { CreatedAt, Amount, PaidAt }

