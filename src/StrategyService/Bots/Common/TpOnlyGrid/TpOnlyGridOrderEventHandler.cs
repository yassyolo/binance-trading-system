using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridOrderEventHandler<TOptions>(
    TOptions options, 
    IPositionStore positionStore, 
    IClock clock)
    : IBotOrderEventHandler where TOptions : class
    , ITpOnlyGridBotOptions
{
    public string BotName => options.BotName;
    
    public async Task HandleTpFilledAsync(string id, decimal qty, CancellationToken ct)
    {
        var position = await positionStore.GetAsync(BotName, id, ct);  
        if(position is null || position.Closed)
            return;
        
        position.MarkTpFilled(qty, clock.UtcNow);
        
        await positionStore.SaveAsync(position, ct);
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
