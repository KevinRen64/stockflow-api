namespace StockFlow.Application.Common.Exceptions;

public class AppException : Exception
{
  public string Code { get; }

  protected AppException(string code, string message) : base(message)
  {
    Code = code;
  }
}