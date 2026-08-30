using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012OrderEventHandler(
    IOptions<Bot8012Options> options, 
    IPositionStore positionStore, 
    IClock clock, 
    ILogger<Bot8012OrderEventHandler> logger)
    :IBotOrderEventHandler
{
    private readonly Bot8012Options _o = options.Value;
    
    public string BotName => _o.BotName;
    
    public async Task HandleTpFilledAsync(string id, decimal qty, CancellationToken ct)
    {
        var position = await positionStore.GetAsync(BotName, id, ct);    
        if(position is null)
            return;
        
        position.MarkTpFilled(qty, clock.UtcNow);
        
        await positionStore.SaveAsync(position, ct);
        
        logger.LogInformation("TP filled. Bot = {Bot} Position = {Position}", BotName, id);
    }

    public async Task HandleTpTerminalAsync(string id, string status, CancellationToken ct)
    {
        var position = await positionStore.GetAsync(BotName, id, ct);
        if (position is null || position.Closed)
            return;

        position.MarkTpOrderTerminal(status, clock.UtcNow);

        await positionStore.SaveAsync(position, ct);
    }

    public Task HandleSlTriggeredAsync(string id, CancellationToken ct) 
        => Task.CompletedTask; 
    
    public Task HandleStop3TriggeredAsync(string id, CancellationToken ct) 
        => Task.CompletedTask;
}
