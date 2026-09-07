
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TradingSystem.Dashboard.Api.Authentication.Models;

namespace TradingSystem.Dashboard.Api.Authentication;

public sealed class DashboardTokenService(DashboardTokenOptions options)
{
    private static readonly string[] AdministratorRoles =
    [
        "Viewer",
        "Operator",
        "Administrator"
    ];

    public DashboardLoginResponse CreateAdministratorToken(string userName)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddHours(options.LifetimeHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userName),
            new("name", userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(AdministratorRoles.Select(role => new Claim("role", role)));

        var credentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        options.SigningKey)),
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAtUtc,
                signingCredentials: credentials);

        var serialized =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new DashboardLoginResponse(
            serialized,
            "Bearer",
            expiresAtUtc,
            userName,
            userName,
            AdministratorRoles);
    }
}
