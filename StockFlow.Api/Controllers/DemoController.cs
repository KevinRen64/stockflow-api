using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Demo;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/demo")]
public class DemoController : ControllerBase
{
    [HttpPost]
    public IActionResult Post(DemoRequest req) => Ok(new { req.Name });
}