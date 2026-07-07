using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8015TradeExecutor(
    IOptions<Bot8015Options> options,
    OrderExecutionService orders,
    IPositionStore positionStore,
    ILogger<Bot8015TradeExecutor> logger)
    : IBotTradeExecutor
{
    private readonly Bot8015Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task OpenAsync(
    string symbol,
    PositionSide side,
    string? source,
    CancellationToken cancellationToken)
    {
        var position = await orders.OpenStop3TrailingPositionAsync(
            _options.BotName,
            side,
            _options.Symbol,
            _options.Quantity,
            _options.InitialStopLoss,
            _options.TpPercent,
            cancellationToken);

        position.Source = string.IsNullOrWhiteSpace(source)
            ? "webhook"
            : source;

        await positionStore.SaveAsync(position, cancellationToken);

        logger.LogInformation(
            "BOT8015 opened STOP3 position. ShortId={ShortId}, Side={Side}, Symbol={Symbol}",
            position.ShortId,
            position.Side,
            position.Symbol);
    }
    public async Task CloseAsync(
        string shortId,
        string reason,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            _options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        await orders.ClosePositionAsync(
            _options.BotName,
            position,
            cancellationToken);

        position.MarkClosed(reason);

        await positionStore.SaveAsync(position, cancellationToken);
    }
}