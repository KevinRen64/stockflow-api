namespace StockFlow.Application.Auth;

public record RegisterRequest
(
  string Email,
  string Password
);