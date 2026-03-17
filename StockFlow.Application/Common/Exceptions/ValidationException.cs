namespace StockFlow.Application.Common.Exceptions;

public class ValidationException : AppException
{
  public ValidationException(string message, string code = "validation_error") : base(code, message)
  {
    
  }
}