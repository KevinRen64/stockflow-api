using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StockFlow.Application.Common;
using StockFlow.Infrastructure.Identity;

namespace StockFlow.Application.Auth;

public class AuthService : IAuthService
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly IConfiguration _configuration;

  public AuthService(
      UserManager<ApplicationUser> userManager, 
      IConfiguration configuration)
  {
    _userManager = userManager;
    _configuration = configuration;
  }

  public async Task<Result<string>> RegisterAsync (RegisterRequest req)
  {
    var existingUser = await _userManager.FindByEmailAsync(req.Email);
    if(existingUser != null)
    {
      return Result<string>.Failure("Email already eists.", "email_already_exists");
    }

    var user = new ApplicationUser
    {
      UserName = req.Email,
      Email = req.Email
    };

    var result = await _userManager.CreateAsync(user, req.Password);

    if(!result.Succeeded)
    {
      var details = string.Join(";", result.Errors.Select(e => e.Description));
      return Result<string>.Failure($"User created but assigning role failed: {details}", "role_assignment_failed");
    }

    var roleResult = await _userManager.AddToRoleAsync(user, "User");
    if(!roleResult.Succeeded)
    {
      var details = string.Join("; ", roleResult.Errors.Select(e => e.Description));
      return Result<string>.Failure($"User created but assigning role failed: {details}", "role_assignment_failed");
    }

    return Result<string>.Success("User registered successfully.");
  }

  public async Task<Result<AuthResponse>> LoginAsync (LoginRequest req)
  {
    var user = await _userManager.FindByEmailAsync(req.Email);
    if(user == null)
    {
      return Result<AuthResponse>.Failure("Invalid email or password. ", "invalid_credentials");
    }

    var validPassword = await _userManager.CheckPasswordAsync(user, req.Password);
    if(!validPassword)
    {
      return Result<AuthResponse>.Failure("Invalid email or password. ", "invalid_credentials");
    }

    var roles = await _userManager.GetRolesAsync(user);

    var claims = new List<Claim>
    {
      new Claim(JwtRegisteredClaimNames.Sub, user.Id),
      new Claim(JwtRegisteredClaimNames.Email, user.Email),
      new Claim(ClaimTypes.NameIdentifier, user.Id),
      new Claim(ClaimTypes.Name, user.Email!)   
    };

    foreach(var role in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var jwtSection = _configuration.GetSection("Jwt");
    var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSection["Key"]!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var response = new AuthResponse(
            tokenString,
            user.Email!,
            roles
        );

        return Result<AuthResponse>.Success(response);
  }
}