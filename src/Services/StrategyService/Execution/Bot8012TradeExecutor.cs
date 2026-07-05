using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8012TradeExecutor : IBotTradeExecutor
{
    private readonly Bot8012Options _options;
    private readonly OrderExecutionService _orders;
    private readonly IPositionStore _positionStore;
    private readonly ILogger<Bot8012TradeExecutor> _logger;

    public string BotName => _options.BotName;

    public Bot8012TradeExecutor(
        IOptions<Bot8012Options> options,
        OrderExecutionService orders,
        IPositionStore positionStore,
        ILogger<Bot8012TradeExecutor> logger)
    {
        _options = options.Value;
        _orders = orders;
        _positionStore = positionStore;
        _logger = logger;
    }

    public async Task OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string source,
        CancellationToken cancellationToken)
    {
        var position = await _orders.OpenTpOnlyPositionAsync(
            _options.BotName,
            side,
            _options.Symbol,
            _options.Quantity,
            _options.ProfitDistance,
            cancellationToken);

        position.Source = source;

        await _positionStore.SaveAsync(position, cancellationToken);

        _logger.LogInformation(
            "BOT8012 opened position. Position={ShortId}, Side={Side}, Symbol={Symbol}",
            position.ShortId,
            position.Side,
            position.Symbol);
    }
}