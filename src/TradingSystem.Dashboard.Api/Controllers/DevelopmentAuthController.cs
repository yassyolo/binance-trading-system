using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/dev-auth")]
[AllowAnonymous]
public sealed class DevelopmentAuthController(
    IConfiguration configuration,
    IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost("token")]
    public IActionResult CreateToken()
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var issuer = configuration["Authentication:Issuer"]
            ?? throw new InvalidOperationException(
                "Authentication:Issuer is not configured.");

        var audience = configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException(
                "Authentication:Audience is not configured.");

        var signingKey = configuration["Authentication:SigningKey"]
            ?? throw new InvalidOperationException(
                "Authentication:SigningKey is not configured.");

        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "local-development-user"),
            new(ClaimTypes.Name, "Local Developer"),
            new(ClaimTypes.Role, "Administrator"),
            new(ClaimTypes.Role, "Operator"),
            new(ClaimTypes.Role, "Viewer"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddHours(12),
            signingCredentials: credentials);

        var serializedToken =
            new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            accessToken = serializedToken,
            tokenType = "Bearer",
            expiresAtUtc = now.AddHours(12),
            roles = new[]
            {
                "Viewer",
                "Operator",
                "Administrator"
            }
        });
    }
}