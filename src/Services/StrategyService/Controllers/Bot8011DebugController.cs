using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using System.Runtime.InteropServices;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders;

namespace StrategyService.Controllers;

[ApiController]
[Route("api/bot8011")]
public sealed class Bot8011DebugController : ControllerBase
{
    private readonly Bot8011Options _options;
    private readonly IPositionStore _positionStore;
    private readonly IBinanceFuturesOrderClient _orders;

    public Bot8011DebugController(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _orders = orders;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            bot = _options.BotName,
            symbol = _options.Symbol,
            timeUtc = DateTime.UtcNow
        });
    }

    [HttpGet("positions")]
    public async Task<IActionResult> Positions(CancellationToken cancellationToken)
    {
        var redisPositions = await _positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        var binancePositions = await _orders.GetPositionRiskAsync(
            _options.Symbol,
            cancellationToken);

        var openOrders = await _orders.GetOpenOrdersAsync(
            _options.Symbol,
            cancellationToken);

        var openAlgoOrders = await _orders.GetOpenAlgoOrdersAsync(
            _options.Symbol,
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