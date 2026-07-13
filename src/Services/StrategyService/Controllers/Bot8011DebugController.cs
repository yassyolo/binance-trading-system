using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using System.Runtime.InteropServices;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Controllers;

[ApiController]
[Route("api/bot8011")]
public sealed class Bot8011DebugController(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders) : ControllerBase
{
    private readonly Bot8011Options options = options.Value;

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            bot = options.BotName,
            symbol = options.Symbol,
            timeUtc = DateTime.UtcNow
        });
    }

    [HttpGet("positions")]
    public async Task<IActionResult> Positions(CancellationToken cancellationToken)
    {
        var redisPositions = await positionStore.GetAllAsync(
            options.BotName,
            cancellationToken);

        var binancePositions = await orders.GetPositionRiskAsync(
            options.Symbol,
            cancellationToken);

        var openOrders = await orders.GetOpenOrdersAsync(
            options.Symbol,
            cancellationToken);

        var openAlgoOrders = await orders.GetOpenAlgoOrdersAsync(
            options.Symbol,
            cancellationToken);

        return Ok(new
        {
            redisPositions,
            binancePositions,
            openOrders,
            openAlgoOrders
        });
    }
}