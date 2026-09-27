namespace Pos.SalesService.Domain.Constants;

public static class SaleStatus
{
    public const string Draft = "Draft";
    public const string PendingPayment = "PendingPayment";
    public const string Completing = "Completing";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string PartiallyReturned = "PartiallyReturned";
    public const string Returned = "Returned";
}
