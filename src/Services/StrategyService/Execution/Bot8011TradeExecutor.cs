using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Positions;
using TradingSystem.Domain.Enums;

namespace StrategyService.Execution;

public sealed class Bot8011TradeExecutor(
        IOptions<Bot8011Options> options,
        OrderExecutionService orders,
        IPositionStore positionStore,
        ILogger<Bot8011TradeExecutor> logger)
    : IBotTradeExecutor
{
    private readonly Bot8011Options options = options.Value;

    public string BotName => options.BotName;

    public async Task OpenAsync(
        string symbol,
        PositionSide side,
        string? source,
        CancellationToken cancellationToken)
    {
        var position = await orders.OpenBot8011PositionAsync(
            side,
            options,
            cancellationToken);

        position.Source = string.IsNullOrWhiteSpace(source)
    ? "webhook"
    : source;

        await positionStore.SaveAsync(position, cancellationToken);

        logger.LogInformation(
            "BOT8011 opened position. ShortId={ShortId}, Side={Side}, Source={Source}",
            position.ShortId,
            position.Side,
            position.Source);
    }

    public async Task CloseAsync(
        string shortId,
        string reason,
        CancellationToken cancellationToken)
    {
        var position = await positionStore.GetAsync(
            options.BotName,
            shortId,
            cancellationToken);

        if (position is null)
        {
            logger.LogWarning(
                "BOT8011 close skipped. Position not found. ShortId={ShortId}, Reason={Reason}",
                shortId,
                reason);

            return;
        }

        if (position.Closed)
        {
            logger.LogInformation(
                "BOT8011 close skipped. Position already closed. ShortId={ShortId}",
                shortId);

            return;
        }

        await orders.ClosePositionAsync(
            options.BotName,
            position,
            cancellationToken);

        position.MarkClosed(reason);

        await positionStore.SaveAsync(position, cancellationToken);

        logger.LogInformation(
            "BOT8011 closed position. ShortId={ShortId}, Reason={Reason}",
            shortId,
            reason);
    }
}