using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8012TradeExecutor(
    IOptions<Bot8012Options> options,
    OrderExecutionService orders,
    IPositionStore positionStore,
    ILogger<Bot8012TradeExecutor> logger) 
    : IBotTradeExecutor
{
    private readonly Bot8012Options options = options.Value;

    public string BotName => options.BotName;

    public async Task OpenAsync(
        string botName,
        string symbol,
        PositionSide side,
        string source,
        CancellationToken cancellationToken)
    {
        var position = await orders.OpenTpOnlyPositionAsync(
            options.BotName,
            side,
            options.Symbol,
            options.Quantity,
            options.ProfitDistance,
            cancellationToken);

        position.Source = source;

        await positionStore.SaveAsync(position, cancellationToken);

        logger.LogInformation(
            "BOT8012 opened position. Position={ShortId}, Side={Side}, Symbol={Symbol}",
            position.ShortId,
            position.Side,
            position.Symbol);
    }

    public async Task CloseAsync(
    string botName,
    string shortId,
    string reason,
    CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            options.BotName,
            shortId,
            cancellationToken);

        if (position is null || position.Closed)
            return;

        await orders.ClosePositionAsync(
            options.BotName,
            position,
            cancellationToken);

        position.MarkClosed(reason);

        await positionStore.SaveAsync(position, cancellationToken);
    }
}