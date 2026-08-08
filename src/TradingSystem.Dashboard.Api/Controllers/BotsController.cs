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
    IBotConfigurationStore configurationStore,
    IBotCommandStore commandStore)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var result = await configurationStore.GetAllAsync(
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{botName}")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(
        string botName,
        CancellationToken cancellationToken)
    {
        RequestValidation.ValidateBotName(botName);

        var result = await configurationStore.GetAsync(
            botName,
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{botName}/configuration")]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> UpdateConfigurationAsync(
        string botName,
        UpdateBotConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidation.ValidateBotName(botName);
        RequestValidation.Validate(request);

        try
        {
            var result = await configurationStore.UpdateAsync(
                botName,
                request,
                DashboardUserName,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            throw new ApiConflictException(
                exception.Message);
        }
    }

    [HttpPost("{botName}/commands")]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("dangerous")]
    public async Task<IActionResult> EnqueueCommandAsync(
        string botName,
        BotCommandRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidation.ValidateBotName(botName);
        RequestValidation.Validate(request);

        var result = await commandStore.EnqueueAsync(
            botName,
            request,
            DashboardUserName,
            cancellationToken);

        return Accepted(value: result);
    }
}
