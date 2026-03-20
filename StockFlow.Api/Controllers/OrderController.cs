using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Common;
using StockFlow.Application.Orders;

namespace StockFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
  private readonly IOrderService _service;

  public OrderController (IOrderService service)
  {
    _service = service;
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
  {
    if(!Request.Headers.TryGetValue("Idempotency-Key", out var keyValues))
    {
      return BadRequest(new
      {
        error = "missing_idempotency_key",
        message = "Idempotency-Key header is required."
      });
    }

    var idempotencyKey = keyValues.ToString().Trim();

    if(string.IsNullOrWhiteSpace(idempotencyKey))
    {
      return BadRequest(new
      {
        error = "missing_idempotency_key",
        message = "Idempotency-Key header is required."
      });
    }

    var result = await _service.CreateAsync(req, idempotencyKey, ct);
    
    if(!result.IsSuccess)
    {
      return MapFailure(result);
    }

    return CreatedAtAction(
      nameof(GetById),
      new { id = result.Value!.Id},
      result.Value
    );
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
  {
    var result = await _service.GetByIdAsync(id, ct);

    if(!result.IsSuccess)
    {
      return MapFailure(result);
    }

    return Ok(result.Value);
  }

  [HttpGet]
  public async Task<ActionResult<PagedResult<OrderDto>>> GetAll(
    [FromQuery] string? status, 
    [FromQuery] string? keyword, 
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 10, 
    [FromQuery] string? sortBy = "createdAt", 
    [FromQuery] bool desc = true, 
    CancellationToken ct = default)
  {
    var orders = await _service.GetAllAsync(status, keyword, page, pageSize, sortBy, desc, ct);
    return Ok(orders);
  }

  [HttpPost("{id:guid}/cancel")]
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
  {
    var result = await _service.CancelAsync(id, ct);

    if(!result.IsSuccess)
    {
      return MapFailure(result);
    }
    return Ok(result.Value);
  }

  [HttpPost("{id:guid}/confirm")]
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
  {
    var result = await _service.ConfirmAsync(id, ct);

    if(!result.IsSuccess)
    {
      return MapFailure(result);
    }
    return Ok(result.Value);
  }

  [HttpPost("{id:guid}/ship")]
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Ship(Guid id, CancellationToken ct)
  {
    var result = await _service.ShipAsync(id, ct);

    if(!result.IsSuccess)
    {
      return MapFailure(result);
    }

    return Ok(result.Value);
  }

  [HttpPost("{id:guid}/complete")]
  [Authorize(Roles = "Admin")]
  public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
  {
    var result = await _service.CompleteAsync(id, ct);

    if(!result.IsSuccess)
    {
      return MapFailure(result);
    }

    return Ok(result.Value);
  }

  private IActionResult MapFailure<T>(StockFlow.Application.Common.Result<T> result)
  {
    return result.ErrorCode switch
    {
      "product_not_found" => NotFound(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "inventory_not_found" => NotFound(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "order_not_found" => NotFound(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "inventory_concurrency_conflict" => Conflict(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "order_conflict" => Conflict(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "insufficient_stock" => Conflict(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "insufficient_onhand_stock" => Conflict(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "invalid_reserved_stock" => Conflict(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "idempotency_key_payload_mismatch" => Conflict(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "idempotency_request_failed" => Conflict(new
      {
        error = result.Error,
        code = result.ErrorCode
      }),

      "request_in_progress" => Conflict(new
      {
          error = result.Error,
          code = result.ErrorCode
      }),

      _ =>  BadRequest(new
      {
        error = result.Error,
        code = result.ErrorCode
      })
    };
  }
}