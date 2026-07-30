using Microsoft.Extensions.Options; 
using TradingSystem.Application.Orders; 
using TradingSystem.Application.Positions; 
using TradingSystem.Application.Time;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012OrderEventHandler(
    IOptions<Bot8012Options> options, 
    IPositionStore store, IClock clock, 
    ILogger<Bot8012OrderEventHandler> logger)
    :IBotOrderEventHandler
{
    private readonly Bot8012Options _o = options.Value;
    
    public string BotName => _o.BotName;
    
    public async Task HandleTpFilledAsync(string id, decimal qty, CancellationToken ct)
    {
        var p = await store.GetAsync(BotName, id, ct);
        
        if(p is null)
            return;
        
        p.MarkTpFilled(qty, clock.UtcNow);
        
        await store.SaveAsync(p, ct);logger.LogInformation("TP filled. Bot = {Bot} Position = {Position}", BotName, id);
    }

    public async Task HandleTpTerminalAsync(string id, string status, CancellationToken ct)
    {
        var p = await store.GetAsync(BotName, id, ct);

        if (p is null || p.Closed)
            return;

        p.MarkTpOrderTerminal(status, clock.UtcNow);

        await store.SaveAsync(p, ct);
    }

    public Task HandleSlTriggeredAsync(string id, CancellationToken ct) 
        => Task.CompletedTask; 
    
    public Task HandleStop3TriggeredAsync(string id, CancellationToken ct) 
        => Task.CompletedTask;
}
