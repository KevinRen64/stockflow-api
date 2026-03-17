namespace StockFlow.Application.Common.Exceptions;

public class ConflictException : AppException
{
  public ConflictException(string message, string code = "conflict") : base(code, message)
  {
    
  }
}