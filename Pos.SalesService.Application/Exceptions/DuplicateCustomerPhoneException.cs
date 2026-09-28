namespace Pos.SalesService.Application.Exceptions;

public class DuplicateCustomerPhoneException : Exception
{
    public DuplicateCustomerPhoneException(string message, Exception innerException)
        : base(message, innerException) { }
}
