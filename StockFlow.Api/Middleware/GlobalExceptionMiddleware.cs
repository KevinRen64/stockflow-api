using System.Net;
using System.Text.Json;
using StockFlow.Application.Common;
using StockFlow.Application.Common.Exceptions;

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
      await HandleExceptionAsync(context, ex);
    }
  }

  private async Task HandleExceptionAsync(HttpContext context, Exception ex)
  {
    var traceId = context.TraceIdentifier;

    _logger.LogError(ex,
      "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}",
      traceId,
      context.Request.Path
    );

    context.Response.ContentType = "application/json";

    var response = new ApiErrorResponse
    {
      TraceId = traceId
    };

    switch(ex)
    {
      case NotFoundException notFoundEx:
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        response.Code = notFoundEx.Code;
        response.Message = notFoundEx.Message;
        break;
      
      case ConflictException conflictEx:
        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
        response.Code = conflictEx.Code;
        response.Message = conflictEx.Message;
        break;

      case ValidationException validationEx:
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        response.Code = validationEx.Code;
        response.Message = validationEx.Message;
        break;

      case AppException appEx:
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        response.Code = appEx.Code;
        response.Message = appEx.Message;
        break;

      default:
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        response.Code = "internal_server_error";
        response.Message = _environment.IsDevelopment()
            ? ex.Message
            : "An unexpected error occurred.";
        break;
    }

    var json = JsonSerializer.Serialize(response);
    await context.Response.WriteAsync(json);
  }
}