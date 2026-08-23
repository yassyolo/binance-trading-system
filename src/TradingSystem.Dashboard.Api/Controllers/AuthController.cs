
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Authentication;
using TradingSystem.Dashboard.Api.Authentication.Models;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    DashboardCredentials credentials,
    DashboardTokenService tokenService)
    : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public IActionResult Login(
        DashboardLoginRequest request)
    {
        var userName =
            request.UserName?.Trim() ?? string.Empty;

        var password =
            request.Password ?? string.Empty;

        if (userName.Length is < 1 or > 100 ||
            password.Length is < 1 or > 256)
        {
            return Unauthorized(
                InvalidCredentials());
        }

        var validUser =
            DashboardPasswordVerifier.FixedTimeEquals(
                userName,
                credentials.UserName);

        // Always verify the password hash even when the user name is wrong.
        // This keeps the failure path closer in cost and avoids a simple
        // user-name timing oracle.
        var validPassword =
            DashboardPasswordVerifier.Verify(
                password,
                credentials.PasswordHash);

        if (!validUser ||
            !validPassword)
        {
            return Unauthorized(
                InvalidCredentials());
        }

        return Ok(
            tokenService.CreateAdministratorToken(
                credentials.UserName));
    }

    [HttpGet("me")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public IActionResult Me()
    {
        var userName =
            User.FindFirst("sub")?.Value
            ?? User.Identity?.Name
            ?? "dashboard-user";

        var displayName =
            User.FindFirst("name")?.Value
            ?? userName;

        var roles =
            User.FindAll("role")
                .Select(x => x.Value)
                .Distinct(
                    StringComparer.Ordinal)
                .OrderBy(x => x)
                .ToArray();

        return Ok(
            new DashboardUserResponse(
                userName,
                displayName,
                roles));
    }

    private static ProblemDetails
        InvalidCredentials() =>
        new()
        {
            Status =
                StatusCodes.Status401Unauthorized,
            Title =
                "Authentication failed",
            Detail =
                "The supplied dashboard credentials are invalid.",
            Type =
                "https://trading-system/errors/authentication"
        };
}
