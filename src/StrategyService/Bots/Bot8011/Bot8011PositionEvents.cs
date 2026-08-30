using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Locking;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Observability.Pipeline;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011PositionEvents(
    IOptions<Bot8011Options> options,
    IPositionStore positionStore,
    IPositionLockProvider locks, 
    Bot8011Stop3OrderService stop3, 
    SafeBinanceOrderService safe, 
    IClock clock, 
    ITradingPipelineRecorder history)
    :TradingSystem.Application.Orders.IBotOrderEventHandler
{
    readonly Bot8011Options o = options.Value;
    
    public string BotName => o.BotName;
    
    public async Task HandleTpFilledAsync(string id, decimal qty, CancellationToken ct)
    {
        await using var @lock = await locks.TryAcquireAsync(BotName, id, TimeSpan.FromSeconds(30), ct);
        if(@lock is null)
            return;
        
        var position = await positionStore.GetAsync(BotName, id, ct);
        if(position is null || position.Closed || position.TpExecuted)
            return;
        
        var now = clock.UtcNow;
        position.MarkTpFilled(qty, now);
        
        if(position.RemainingQuantity <= 0)
        {
            position.MarkClosed("TP_FULL_EXIT", now);
            await positionStore.SaveAsync(position, ct);
            
            return;
        }
        
        position.Stop3Pending = true;
        position.Status = PositionStatus.Stop3Pending;
        
        await positionStore.SaveAsync(position, ct);
        
        try
        {
            var x = await stop3.CreateInitialAsync(position, position.RemainingQuantity, ct);position.Stop3ClientId = x.ClientAlgoId;
            
            position.Stop3OrderId = x.AlgoOrderId;
            position.Stop3Initial = position.Stop3Current = position.Stop3Previous = x.TriggerPrice;
            position.Stop3Status = x.Status;
            position.Stop3Created = true;
            position.Stop3Pending = false;
            position.Status = PositionStatus.Stop3Active;
            
            await positionStore.SaveAsync(position, ct);
            
            if(!string.IsNullOrWhiteSpace(position.SlOrderId))
            {
                await safe.SafeCancelAlgoAsync(position.Symbol, position.SlOrderId, position.SlClientId, ct);
                
                position.SlStatus = "CANCELED";
                await positionStore.SaveAsync(position, ct);
            }
        }
        catch
        {
            position.Stop3Pending = true;
            position.Stop3Created = false;
            position.ProtectiveActive = true;
            
            await positionStore.SaveAsync(position, ct);
            
            throw;
        }
        
        await history.RecordPositionEventAsync(new(position.ShortId, position.BotName, "TakeProfitFilled", "Filled", now, position.TpPrice, qty), ct);
    }
    
    public Task HandleTpTerminalAsync(string id, string status, CancellationToken ct) 
        => Task.CompletedTask;
    
    public Task HandleSlTriggeredAsync(string id, CancellationToken ct) 
        => Close(id, "SL_TRIGGERED", ct);
    
    public Task HandleStop3TriggeredAsync(string id, CancellationToken ct) 
        => Close(id, "STOP3_TRIGGERED", ct);
    
    async Task Close(string id, string reason, CancellationToken ct)
    {
        await using var @lock = await locks.TryAcquireAsync(BotName, id, TimeSpan.FromSeconds(30), ct);
        if(@lock is null)
            return;
        
        var p = await positionStore.GetAsync(BotName, id, ct);
        if(p is null || p.Closed)
            return;
        
        var now = clock.UtcNow;
        
        p.ProtectiveActive = false;
        p.Stop3Pending = false;
        p.TrailingInProgress = false;
        p.MarkClosed(reason, now);
        
        await positionStore.SaveAsync(p, ct);
        
        await history.RecordPositionEventAsync(
            new(p.ShortId, 
                p.BotName, 
                reason, 
                "Closed", 
                now, 
                p.Stop3Current, 
                p.RemainingQuantity),
            ct);
    }
}
