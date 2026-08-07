using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8015.Configuration;
using TradingSystem.Application.Locking;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;
using TradingSystem.Observability.Pipeline;

namespace StrategyService.Bots.Bot8015
{
    public sealed class Bot8015PositionEvents(
        IOptions<Bot8015Options> options,
        IPositionStore store,
        IPositionLockProvider locks,
        Bot8015Stop3OrderService stop3,
        SafeBinanceOrderService safe,
        IClock clock,
        ITradingPipelineRecorder history) : 
        TradingSystem.Application.Orders.IBotOrderEventHandler
    {
        readonly Bot8015Options o = options.Value;

        public string BotName => o.BotName;

        public async Task HandleTpFilledAsync(string id, decimal qty, CancellationToken ct)
        {
            await using var l = await locks.TryAcquireAsync(BotName, id, TimeSpan.FromSeconds(30), ct);
            if (l is null) return;
            
            var p = await store.GetAsync(BotName, id, ct);
            if (p is null || p.Closed || p.TpExecuted) return;
           
            var now = clock.UtcNow;
            
            p.MarkTpFilled(qty, now);
            
            if (p.RemainingQuantity <= 0)
            {
                p.MarkClosed("TP_FULL_EXIT", now);
                await store.SaveAsync(p, ct);
                return;
            }
            
            p.Stop3Pending = true;     
            p.Status = PositionStatus.Stop3Pending;
            
            await store.SaveAsync(p, ct);
           
            try
            {
                var x = await stop3.CreateInitialAsync(p, p.RemainingQuantity, ct);
                
                p.Stop3ClientId = x.ClientAlgoId;
                p.Stop3OrderId = x.AlgoOrderId;
                p.Stop3Initial = p.Stop3Current = p.Stop3Previous = x.TriggerPrice;
                p.Stop3Status = x.Status;
                p.Stop3Created = true;
                p.Stop3Pending = false;
                p.Status = PositionStatus.Stop3Active;
                
                await store.SaveAsync(p, ct);
                
                if (!string.IsNullOrWhiteSpace(p.SlOrderId))
                {
                    await safe.SafeCancelAlgoAsync(p.Symbol, p.SlOrderId, p.SlClientId, ct);
                    
                    p.SlStatus = "CANCELED";
                   
                    await store.SaveAsync(p, ct);
                }
            }
            catch
            {
                p.Stop3Pending = true;
                p.Stop3Created = false;
                p.ProtectiveActive = true;
                await store.SaveAsync(p, ct);
                throw;
            }
            await history.RecordPositionEventAsync(new(p.ShortId, p.BotName, "TakeProfitFilled", "Filled", now, p.TpPrice, qty), ct);
        }

        public Task HandleTpTerminalAsync(string id, string status, CancellationToken ct) => Task.CompletedTask;
        public Task HandleSlTriggeredAsync(string id, CancellationToken ct) => Close(id, "SL_TRIGGERED", ct);
        public Task HandleStop3TriggeredAsync(string id, CancellationToken ct) => Close(id, "STOP3_TRIGGERED", ct);

        async Task Close(string id, string reason, CancellationToken ct)
        {
            await using var l = await locks.TryAcquireAsync(BotName, id, TimeSpan.FromSeconds(30), ct);
            if (l is null) return;
            var p = await store.GetAsync(BotName, id, ct);
            if (p is null || p.Closed) return;
            var now = clock.UtcNow;
            p.ProtectiveActive = false;
            p.Stop3Pending = false;
            p.TrailingInProgress = false;
            p.MarkClosed(reason, now);
            await store.SaveAsync(p, ct);
            await history.RecordPositionEventAsync(new(p.ShortId, p.BotName, reason, "Closed", now, p.Stop3Current, p.RemainingQuantity), ct);
        }
    }
}
