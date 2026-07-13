using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Orders;

namespace StrategyService.Orders;

public sealed class Bot8013OrderEventHandler(
    IOptions<Bot8013Options> options,
    Bot8013PositionEventService events)
    : IBotOrderEventHandler
{
    private readonly Bot8013Options _options = options.Value;

    public string BotName => _options.BotName;

    public Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
        => events.HandleTpFilledAsync(
            shortId,
            executedQuantity,
            cancellationToken);

    public Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken)
        => events.HandleTpTerminalAsync(
            shortId,
            status,
            cancellationToken);

    public Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
