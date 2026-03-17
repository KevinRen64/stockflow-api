using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Products;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Data;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
  private readonly IProductService _service;

  public ProductsController(IProductService service)
  {
    _service = service;
  }


  [HttpPost]
  public async Task<IActionResult> Create(CreateProductRequest req, CancellationToken ct)
  {
    var result = await _service.CreateAsync(req, ct);
    if(!result.IsSuccess)
    {
      return result.ErrorCode switch
      {
        "duplicate_sku" => Conflict(new { message = result.Error }),
        _ => BadRequest(new { message = result.Error })
      };
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
      return NotFound(new { message = result.Error });
    }

    return Ok(result.Value);
  }
}