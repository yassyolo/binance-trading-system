using StrategyService.Services;
using TradingSystem.Application.Orders;

namespace StrategyService.Orders;

public sealed class Bot8011OrderEventHandler(
    Bot8011PositionEventService events) : IBotOrderEventHandler
{
    public string BotName => "BOT8011";

    public Task HandleTpFilledAsync(string shortId, decimal executedQuantity, CancellationToken ct)
        => events.HandleTpFilledAsync(shortId, executedQuantity, ct);

    public Task HandleSlTriggeredAsync(string shortId, CancellationToken ct)
        => events.HandleSlTriggeredAsync(shortId, ct);

    public Task HandleStop3TriggeredAsync(string shortId, CancellationToken ct)
        => events.HandleStop3TriggeredAsync(shortId, ct);

    public Task HandleTpTerminalAsync(string shortId, string status, CancellationToken ct)
        => Task.CompletedTask;
}
