using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011TradeExecutor(
    IOptions<Bot8011Options> options, 
    BinanceProtectedPositionService execution, 
    IPositionStore store, 
    IClock clock, 
    ILogger<Bot8011TradeExecutor> logger)
    :IBotTradeExecutor
{
    readonly Bot8011Options o = options.Value;
    
    public string BotName => o.BotName;
    
    public async Task<TradeExecutionResult> OpenAsync(string symbol, PositionSide side, string? source, CancellationToken ct)
    {
        if(!symbol.Equals(o.Symbol, StringComparison.OrdinalIgnoreCase))
            return TradeExecutionResult.Failure($"{BotName} does not support {symbol}.");
        
        try
        {
            var p = await execution.OpenAsync(BotName, symbol, side, o.Quantity, o.TakeProfitPercent, o.InitialStopLossDistance, ct);
            p.Source = string.IsNullOrWhiteSpace(source) ? "internal" : source;
            
            await store.SaveAsync(p, ct);
            
            return TradeExecutionResult.Success(p.ShortId);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "BOT8011 open failed.");
            return TradeExecutionResult.Failure(ex.Message, ex);
        }
    }
    
    public async Task<TradeExecutionResult> CloseAsync(string id, string reason, CancellationToken ct)
    {
        var p = await store.GetAsync(BotName, id, ct);
        if(p is null)
            return TradeExecutionResult.Failure($"Position {id} not found.");
        
        if(p.Closed)
            return TradeExecutionResult.Success(id, "Already closed.");
        
        try
        {
            await execution.CloseAsync(p, ct);
            
            p.MarkClosed(reason, clock.UtcNow);
            
            await store.SaveAsync(p, ct);
            
            return TradeExecutionResult.Success(id, reason);
        }
        catch(Exception ex)
        {
            return TradeExecutionResult.Failure(ex.Message, ex);
        }
    }
}
