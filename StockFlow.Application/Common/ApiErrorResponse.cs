namespace StockFlow.Application.Common;

public class ApiErrorResponse
{
  public string? Error { get; set; } = default!;
  public string? Message { get; set; } = default!;
  public string? TraceId { get; set; }

}