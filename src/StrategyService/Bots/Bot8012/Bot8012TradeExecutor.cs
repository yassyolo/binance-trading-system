using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8012;

public sealed class Bot8012TradeExecutor(
    IOptions<Bot8012Options> options, 
    BinanceTpOnlyPositionService execution, 
    IPositionStore store, 
    IClock clock, 
    IBotRuntimeConfigurationProvider runtimeConfigurationProvider, 
    ILogger<Bot8012TradeExecutor> logger) : IBotTradeExecutor
{
    private readonly Bot8012Options _options = options.Value;
    public string BotName  =>  _options.BotName;

    public async Task<TradeExecutionResult> OpenAsync(string symbol, PositionSide side, string? source, CancellationToken ct)
    {
        var runtime = await runtimeConfigurationProvider.GetAsync(BotName,  ct);
        
        if (!symbol.Equals(runtime?.Symbol ?? _options.Symbol, StringComparison.OrdinalIgnoreCase))
            return TradeExecutionResult.Failure($"Unsupported symbol '{symbol}'.");
        try
        {
            var position = await execution.OpenAsync(
                BotName, 
                symbol, 
                side, 
                runtime?.Quantity ?? _options.Quantity, 
                runtime?.ProfitDistance ?? _options.ProfitDistance, 
                ct);
            position.Source = source;
            
            await store.SaveAsync(position,  ct);
           
            return TradeExecutionResult.Success(position.ShortId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BOT8012 open failed.");
           
            return TradeExecutionResult.Failure(ex.Message,  ex);
        }
    }

    public async Task<TradeExecutionResult> CloseAsync(string shortId, string reason, CancellationToken ct)
    {
        var position = await store.GetAsync(BotName, shortId, ct);
        if (position is null) 
            return TradeExecutionResult.Failure($"Position '{shortId}' not found.");
        
        if (position.Closed) 
            return TradeExecutionResult.Success(shortId, "Already closed.");
       
        try
        {
            await execution.CloseAsync(position, ct);
            
            position.MarkClosed(reason, clock.UtcNow);    
            await store.SaveAsync(position, ct);
           
            return TradeExecutionResult.Success(shortId, reason);
        }
        catch (Exception ex)
        {
            return TradeExecutionResult.Failure(ex.Message, ex);
        }
    }
}
