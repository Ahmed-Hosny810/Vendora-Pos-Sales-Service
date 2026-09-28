using Pos.SalesService.Application.Parameters;
namespace Pos.SalesService.Application.Features.PaymentMethods.Queries.GetAllQuery;
public class GetPaymentMethodsQueryParameter : RequestParameter<PaymentMethodOrderKey>
{
    public PaymentMethodFilter? Filter { get; set; }
}
public class PaymentMethodFilter
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsCash { get; set; }
}
public enum PaymentMethodOrderKey { SortOrder, Name, Code, CreatedAt }
