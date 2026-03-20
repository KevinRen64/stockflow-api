using Serilog.Context;

namespace StockFlow.Api.Middleware;

public class CorrelationIdMiddleware
{
  public const string HeaderName = "X-Correlation-ID";

  private readonly RequestDelegate _next;
  private readonly ILogger<CorrelationIdMiddleware> _logger;

  public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var correlationId = GetOrCreateCorrelationId(context);

    context.TraceIdentifier = correlationId;
    context.Response.Headers[HeaderName] = correlationId;

    using(LogContext.PushProperty("CorrelationId", correlationId))
    {
      _logger.LogInformation("Handling request {Method} {Path} with CorrelationId {CorrelationId}",
        context.Request.Method,
        context.Request.Path,
        correlationId);
      
      await _next(context);
    }
  }

  private static string GetOrCreateCorrelationId(HttpContext context)
  {
    if(context.Request.Headers.TryGetValue(HeaderName, out var existingCorrelationId)
      && !string.IsNullOrWhiteSpace(existingCorrelationId))
    {
      return existingCorrelationId.ToString();
    }
    return Guid.NewGuid().ToString();
  }
}