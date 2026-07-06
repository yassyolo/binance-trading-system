using StrategyService.Services;
using TradingSystem.Application.Orders;

namespace StrategyService.Orders;

public sealed class Bot8012OrderEventHandler(
    Bot8012PositionEventService events) : IBotOrderEventHandler
{
    public string BotName => "BOT8012";

    public Task HandleTpFilledAsync(string shortId, decimal executedQuantity, CancellationToken ct)
        => events.HandleTpFilledAsync(shortId, executedQuantity, ct);

    public Task HandleTpTerminalAsync(string shortId, string status, CancellationToken ct)
        => events.HandleTpTerminalAsync(shortId, status, ct);

    public Task HandleSlTriggeredAsync(string shortId, CancellationToken ct)
        => Task.CompletedTask;

    public Task HandleStop3TriggeredAsync(string shortId, CancellationToken ct)
        => Task.CompletedTask;
}
