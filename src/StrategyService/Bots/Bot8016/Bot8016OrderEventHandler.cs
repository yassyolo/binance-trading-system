using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Application.Orders;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016OrderEventHandler(
    IOptions<Bot8016Options> options, 
    Bot8016PositionLifecycleService lifecycle)
    : IBotOrderEventHandler
{
    private readonly Bot8016Options _options  =  options.Value;

    public string BotName  =>  _options.BotName;

    public Task HandleTpFilledAsync(string shortId,  decimal executedQuantity,  CancellationToken ct)
         =>  lifecycle.HandleTpFilledAsync(
            shortId, 
            executedQuantity, 
            ct);

    public Task HandleTpTerminalAsync(string shortId,  string status,  CancellationToken ct)
         =>  Task.CompletedTask;

    public Task HandleSlTriggeredAsync(string shortId,  CancellationToken ct)
         =>  Task.CompletedTask;

    public Task HandleStop3TriggeredAsync(string shortId,  CancellationToken ct)
         =>  Task.CompletedTask;
}
