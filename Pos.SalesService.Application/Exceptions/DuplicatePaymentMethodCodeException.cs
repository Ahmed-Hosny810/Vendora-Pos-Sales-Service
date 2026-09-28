namespace Pos.SalesService.Application.Exceptions;
public class DuplicatePaymentMethodCodeException : Exception
{
    public DuplicatePaymentMethodCodeException(string message, Exception innerException) : base(message, innerException) { }
}
