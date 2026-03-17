namespace StockFlow.Application.Auth;

public record AuthResponse(
  string Token,
  string Email,
  IList<string> Roles
);