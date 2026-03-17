using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Inventory;

namespace StockFlow.Api.Controllers;


[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
  private readonly IInventoryService _inventoryService;

  public InventoryController(IInventoryService inventoryService)
  {
    _inventoryService = inventoryService;
  }

  [Authorize(Roles = "Admin")]
  [HttpPost("adjust")]
  public async Task<IActionResult> Adjust(AdjustInventoryRequest request, CancellationToken ct)
  {
    var result = await _inventoryService.AdjustAsync(request, ct);
    
    if(!result.IsSuccess)
    {
      return result.ErrorCode switch
        {
            "product_not_found" => NotFound(new { message = result.Error }),
            "inventory_not_initialized" => BadRequest(new { message = result.Error }),
            "negative_inventory" => BadRequest(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }

    return  Ok(result.Value);
  }

  [HttpGet("{productId:guid}")]
  public async Task<IActionResult> GetByProductId(Guid productId, CancellationToken ct)
  {
    var order = await _inventoryService.GetByProductIdAsync(productId, ct);
    return Ok(order);
  }

}