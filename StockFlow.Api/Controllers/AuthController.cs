using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Auth;
using StockFlow.Application.Common;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
  private readonly IAuthService _service;

  public AuthController(IAuthService service)
  {
    _service = service;
  }

  [HttpPost("register")]
  public async Task<IActionResult> Register(RegisterRequest req)
  {
    var result = await _service.RegisterAsync(req);

    if(!result.IsSuccess)
    {
      return BadRequest(new
      {
        error = result.Error,
        code = result.ErrorCode
      });
    }

    return Ok(new
    {
      message = result.Value
    });
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login(LoginRequest req)
  {
    var result = await _service.LoginAsync(req);
    if(!result.IsSuccess)
    {
      return BadRequest(new
      {
        error = result.Error,
        code = result.ErrorCode
      });
    }

    return Ok(result.Value);
  }
}