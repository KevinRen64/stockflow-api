namespace StockFlow.Api.Middleware;

public static class MiddlewareExtensions
{
  public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
  {
    return app.UseMiddleware<GlobalExceptionMiddleware>();
  }

  public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder app)
  {
    return app.UseMiddleware<CorrelationIdMiddleware>();
  }
}