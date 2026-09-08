using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/commands")]
public sealed class CommandsController(
    IBotCommandStore commandStore)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(string? botName, int take, CancellationToken ct)
    {
        if (botName is not null)
            RequestValidation.ValidateBotName(botName);

        var result = await commandStore.GetAsync(botName, RequestValidation.PageSize(take), ct);

        return Ok(result);
    }
}
