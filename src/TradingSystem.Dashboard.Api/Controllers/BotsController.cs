using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Exceptions;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Contracts.Models.Bots;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/bots")]
public sealed class BotsController(
    IBotConfigurationStore configStore,
    IBotCommandStore commandStore)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAllAsync(CancellationToken ct)
    {
        var result = await configStore.GetAllAsync(ct);

        return Ok(result);
    }

    [HttpGet("{botName}")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(string botName, CancellationToken ct)
    {
        RequestValidation.ValidateBotName(botName);

        var result = await configStore.GetAsync(botName, ct);

        return Ok(result);
    }

    [HttpPut("{botName}/configuration")]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateConfigurationAsync(string botName, UpdateBotConfigurationRequest request, CancellationToken ct)
    {
        RequestValidation.ValidateBotName(botName);
        RequestValidation.Validate(request);

        try
        {
            var result = await configStore.UpdateAsync(botName, request, DashboardUserName, ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApiConflictException(ex.Message);
        }
    }

    [HttpPost("{botName}/commands")]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("dangerous")]
    public async Task<IActionResult> EnqueueCommandAsync(string botName, BotCommandRequest request, CancellationToken ct)
    {
        RequestValidation.ValidateBotName(botName);
        RequestValidation.Validate(request);

        var result = await commandStore.EnqueueAsync(botName, request, DashboardUserName, ct);

        return Accepted(value: result);
    }
}
