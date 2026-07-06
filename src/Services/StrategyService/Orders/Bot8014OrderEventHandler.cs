using StrategyService.Services;
using TradingSystem.Application.Orders;

namespace StrategyService.Orders;

public sealed class Bot8014OrderEventHandler(
    Bot8014PositionEventService events)
    : IBotOrderEventHandler
{
    public string BotName => "BOT8014";

    public Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
        => events.HandleTpFilledAsync(shortId, executedQuantity, cancellationToken);

    public Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken)
        => events.HandleTpTerminalAsync(shortId, status, cancellationToken);

    public Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}