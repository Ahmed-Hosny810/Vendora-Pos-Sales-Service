using Pos.SalesService.Application.Parameters;
namespace Pos.SalesService.Application.Features.SalePayments.Queries.GetAllQuery;

public class GetSalePaymentsQueryParameter : RequestParameter<SalePaymentOrderKey>
{
    public SalePaymentFilter? Filter { get; set; }
}
public class SalePaymentFilter
{
    public string? Status { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public bool? IsCash { get; set; }
}
public enum SalePaymentOrderKey { CreatedAt, Amount, PaidAt }

