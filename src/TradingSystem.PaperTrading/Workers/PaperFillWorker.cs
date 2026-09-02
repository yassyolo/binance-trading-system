using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.PaperTrading.Configuration;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Executor;
using TradingSystem.PaperTrading.Models;

namespace TradingSystem.PaperTrading.Workers;

public sealed class PaperFillWorker(
    IPaperTradingStore store, 
    IMarketPriceProvider prices, 
    PaperTradeExecutor executor, 
    IOptions<PaperTradingOptions> options, 
    ILogger<PaperFillWorker> logger) 
    : BackgroundService
{
    private readonly PaperTradingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) 
            return;
        
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(250,  _options.PricePollMilliseconds)));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try 
            { 
                await ProcessAsync(stoppingToken); 
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) 
            { }
            catch (Exception ex) 
            { 
                logger.LogError(ex, "Paper fill cycle failed."); 
            }
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        var positions = await store.GetOpenAsync(ct);
        
        foreach (var group in positions.GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase))
        {
            var markPrice = await prices.GetMarkPriceAsync(group.Key,  ct);
            
            foreach (var position in group)
            {
                var reason = ResolveCloseReason(position,  markPrice);           
                if (reason is null) 
                    continue;
                
                await executor.CloseAsync(position.BotName,  position.ShortId,  reason,  ct);
            }
        }
    }

    public static string? ResolveCloseReason(PaperTradingPosition position,  decimal price)
    {
        if (position.Side == PositionSide.Long)
        {
            if (price >= position.TakeProfitPrice) 
                return "PAPER_TAKE_PROFIT";
            
            if (price <= position.StopLossPrice) 
                return "PAPER_STOP_LOSS";
        }
        else
        {
            if (price <= position.TakeProfitPrice) 
                return "PAPER_TAKE_PROFIT";
            
            if (price >= position.StopLossPrice) 
                return "PAPER_STOP_LOSS";
        }
        return null;
    }
}
