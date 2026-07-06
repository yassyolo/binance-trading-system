using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8014TradeExecutor(
    IOptions<Bot8014Options> options,
    OrderExecutionService orders,
    IPositionStore positionStore,
    ILogger<Bot8014TradeExecutor> logger)
    : IBotTradeExecutor
{
    private readonly Bot8014Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task OpenAsync(
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken)
    {
        var position = await orders.OpenTpOnlyPositionAsync(
            _options.BotName,
            side,
            _options.Symbol,
            _options.Quantity,
            _options.ProfitDistance,
            cancellationToken);

        position.Source = string.IsNullOrWhiteSpace(source)
            ? "webhook"
            : source;

        await positionStore.SaveAsync(position, cancellationToken);

        logger.LogInformation(
            "BOT8013 opened position. ShortId={ShortId}, Side={Side}, Symbol={Symbol}, Source={Source}",
            position.ShortId,
            position.Side,
            position.Symbol,
            position.Source);
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