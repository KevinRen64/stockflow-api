using System.Net;
using System.Text.Json;
using StockFlow.Application.Common;

namespace StockFlow.Api.Middleware;

public class GlobalExceptionMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<GlobalExceptionMiddleware> _logger;
  private readonly IHostEnvironment _environment;

  public GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment
  )
  {
    _next = next;
    _logger = logger;
    _environment = environment;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      var traceId = context.TraceIdentifier;

      _logger.LogError(ex,
        "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}",
        traceId,
        context.Request.Path);

      context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

      var response = new ApiErrorResponse
      {
        Code = "internal_server_error",
        Message = _environment.IsDevelopment()
          ? ex.Message
          : "An unexpected error occured.",
        TraceId = traceId
      };

      var json = JsonSerializer.Serialize(response);

      await context.Response.WriteAsync(json);
    }
  }
}