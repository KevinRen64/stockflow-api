using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Products;


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
  [Authorize(Roles = "Admin")]
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
  [AllowAnonymous]
  public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
  {
    var product = await _service.GetByIdAsync(id, ct);
    return Ok(product);
  }
}