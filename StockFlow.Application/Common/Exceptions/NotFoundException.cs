namespace StockFlow.Application.Common.Exceptions;

public class NotFoundException : AppException
{
  public NotFoundException(string message, string code = "not_found") : base(code, message)
  {
  }
}