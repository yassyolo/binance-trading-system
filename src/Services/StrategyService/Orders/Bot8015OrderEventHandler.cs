using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Orders;

namespace StrategyService.Orders;

public sealed class Bot8015OrderEventHandler(
    IOptions<Bot8015Options> options,
    Bot8015PositionEventService events)
    : IBotOrderEventHandler
{
    private readonly Bot8015Options _options = options.Value;

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
        => events.HandleSlTriggeredAsync(
            shortId,
            cancellationToken);

    public Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => events.HandleStop3TriggeredAsync(
            shortId,
            cancellationToken);
}
