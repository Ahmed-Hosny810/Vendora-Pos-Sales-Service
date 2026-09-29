using Pos.SalesService.Application.Features.Sales.DTOs.Calculations;
using Pos.SalesService.Application.Wrappers;

namespace Pos.SalesService.Application.Interfaces.Services
{
    public interface ISaleCalculationService
    {
        Result<SaleCalculationResult> CalculateSale(
        SaleCalculationInput input);

        Result<PaymentCalculationResult> CalculatePayments(
            PaymentCalculationInput input);

        Result<ReturnCalculationResult> CalculateReturn(
            ReturnCalculationInput input);
    }
}
