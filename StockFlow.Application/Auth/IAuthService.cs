using StockFlow.Application.Common;

namespace StockFlow.Application.Auth;

public interface IAuthService
{
  Task<Result<string>> RegisterAsync(RegisterRequest req);
  Task<Result<AuthResponse>> LoginAsync(LoginRequest req);
}