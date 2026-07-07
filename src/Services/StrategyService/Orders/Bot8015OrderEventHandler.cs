using StrategyService.Services;
using TradingSystem.Application.Orders;

namespace StrategyService.Orders;

public sealed class Bot8015OrderEventHandler(
    Bot8015PositionEventService events)
    : IBotOrderEventHandler
{
    public string BotName => "BOT8015";

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
        => events.HandleSlTriggeredAsync(shortId, cancellationToken);

    public Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => events.HandleStop3TriggeredAsync(shortId, cancellationToken);
}