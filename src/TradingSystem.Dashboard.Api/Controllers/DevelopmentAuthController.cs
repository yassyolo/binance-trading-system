
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingSystem.Dashboard.Api.Authentication;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/dev-auth")]
[AllowAnonymous]
public sealed class DevelopmentAuthController(
    DashboardTokenService tokenService,
    IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost("token")]
    public IActionResult CreateToken()
    {
        if (!environment.IsDevelopment())
            return NotFound();

        return Ok(
            tokenService.CreateAdministratorToken(
                "local-development-user"));
    }
}
