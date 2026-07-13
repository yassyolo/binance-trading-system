using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Orders;

namespace StrategyService.Orders;

public sealed class Bot8016OrderEventHandler(
    IOptions<Bot8016Options> options,
    Bot8016PositionLifecycleService lifecycle)
    : IBotOrderEventHandler
{
    private readonly Bot8016Options _options = options.Value;

    public string BotName => _options.BotName;

    public Task HandleTpFilledAsync(
        string shortId,
        decimal executedQuantity,
        CancellationToken cancellationToken)
        => lifecycle.HandleTpFilledAsync(
            shortId,
            executedQuantity,
            cancellationToken);

    public Task HandleTpTerminalAsync(
        string shortId,
        string status,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task HandleSlTriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task HandleStop3TriggeredAsync(
        string shortId,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
