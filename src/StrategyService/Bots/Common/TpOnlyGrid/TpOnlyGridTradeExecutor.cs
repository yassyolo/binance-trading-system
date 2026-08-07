using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridTradeExecutor<TOptions>(
    TOptions options, 
    BinanceTpOnlyPositionService execution, 
    IPositionStore store, 
    IClock clock, 
    IBotRuntimeConfigurationProvider runtimeConfigurationProvider) :
    IBotTradeExecutor
    where TOptions : class,  ITpOnlyGridBotOptions
{
    public string BotName  =>  options.BotName;

    public async Task<TradeExecutionResult> OpenAsync(string symbol,  PositionSide side,  string? source,  CancellationToken ct)
    {
        var runtime  = await runtimeConfigurationProvider.GetAsync(BotName,  ct);
        var configuredSymbol = runtime?.Symbol ?? options.Symbol;
        
        if (!symbol.Equals(configuredSymbol,  StringComparison.OrdinalIgnoreCase))
            return TradeExecutionResult.Failure($"{BotName} does not support symbol '{symbol}'.");

        var quantity = runtime?.Quantity ?? options.Quantity;
        var profitDistance  =  runtime?.ProfitDistance ?? options.ProfitDistance;
        try
        {
            var position = await execution.OpenAsync(BotName,  symbol,  side,  quantity,  profitDistance,  ct);
            position.Source = string.IsNullOrWhiteSpace(source) ? "external" : source.Trim();
            
            await store.SaveAsync(position,  ct);
            
            return TradeExecutionResult.Success(position.ShortId);
        }
        catch (Exception ex)
        {
            return TradeExecutionResult.Failure($"{BotName} failed to open {side}: {ex.Message}",  ex);
        }
    }

    public async Task<TradeExecutionResult> CloseAsync(string shortId,  string reason,  CancellationToken ct)
    {
        var position = await store.GetAsync(BotName,  shortId,  ct);
        
        if (position is null) 
            return TradeExecutionResult.Failure($"Position '{shortId}' was not found.");
        
        if (position.Closed) 
            return TradeExecutionResult.Success(shortId,  "Position is already closed.");
        
        try
        {
            await execution.CloseAsync(position,  ct);
            
            position.MarkClosed(reason,  clock.UtcNow);
           
            await store.SaveAsync(position,  ct);
            
            return TradeExecutionResult.Success(shortId,  reason);
        }
        catch (Exception ex)
        {
            return TradeExecutionResult.Failure($"{BotName} failed to close '{shortId}': {ex.Message}",  ex);
        }
    }
}
