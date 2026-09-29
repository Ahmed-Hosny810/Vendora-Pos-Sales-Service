
namespace Pos.SalesService.Application.Features.Sales.DTOs.Calculations
{
    public class PaymentCalculationInput
    {
        public decimal SaleTotal { get; set; }

        public List<PaymentCalculationItemInput> Payments { get; set; } = new();
    }

    public class PaymentCalculationItemInput
    {
        public Guid PaymentId { get; set; }

        public bool IsCash { get; set; }

        public string Status { get; set; } = string.Empty;

        // total Money was given by the customer.
        public decimal Amount { get; set; }

        public decimal ChangeAmount { get; set; }
    }

    public class PaymentCalculationResult
    {
        // Sum of completed payment amounts tendered.
        public decimal PaidAmount { get; set; }

        public decimal ChangeAmount { get; set; }

        public decimal NetPaid { get; set; }

        public decimal RemainingDue { get; set; }

        public bool IsFullyPaid { get; set; }
    }
}
