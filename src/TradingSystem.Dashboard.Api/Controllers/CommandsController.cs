using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/commands")]
public sealed class CommandsController(
    IBotCommandStore store)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(
        string? botName,
        int take,
        CancellationToken cancellationToken)
    {
        if (botName is not null)
            RequestValidation.ValidateBotName(botName);

        var result = await store.GetAsync(
            botName,
            RequestValidation.PageSize(take),
            cancellationToken);

        return Ok(result);
    }
}
