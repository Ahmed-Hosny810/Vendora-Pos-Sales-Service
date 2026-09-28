namespace Pos.SalesService.Application.Exceptions;
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}
