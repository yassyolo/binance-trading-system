using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Binance.Execution;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Common.TpOnlyGrid;

public abstract class TpOnlyGridTradeExecutor<TOptions>(
    TOptions options, 
    BinanceTpOnlyPositionService tpOnlyPositionService, 
    IPositionStore positionStore, 
    IClock clock, 
    IBotRuntimeConfigurationProvider configProvider) 
    : IBotTradeExecutor where TOptions : class,  
    ITpOnlyGridBotOptions
{
    public string BotName  =>  options.BotName;

    public async Task<TradeExecutionResult> OpenAsync(string symbol, PositionSide side, string? source, CancellationToken ct)
    {
        var runtime  = await configProvider.GetAsync(BotName,  ct);
        var configuredSymbol = runtime?.Symbol ?? options.Symbol;
        
        if (!symbol.Equals(configuredSymbol, StringComparison.OrdinalIgnoreCase))
            return TradeExecutionResult.Failure($"{BotName} does not support symbol '{symbol}'.");

        var quantity = runtime?.Quantity ?? options.Quantity;
        var profitDistance = runtime?.ProfitDistance ?? options.ProfitDistance;
        
        try
        {
            var position = await tpOnlyPositionService.OpenAsync(BotName, symbol, side, quantity, profitDistance, ct);
            
            position.Source = string.IsNullOrWhiteSpace(source) ? "external" : source.Trim();
            
            await positionStore.SaveAsync(position,  ct);
            
            return TradeExecutionResult.Success(position.ShortId);
        }
        catch (Exception ex)
        {
            return TradeExecutionResult.Failure($"{BotName} failed to open {side}: {ex.Message}", ex);
        }
    }

    public async Task<TradeExecutionResult> CloseAsync(string shortId, string reason, CancellationToken ct)
    {
        var position = await positionStore.GetAsync(BotName, shortId, ct);  
        if (position is null) 
            return TradeExecutionResult.Failure($"Position '{shortId}' was not found.");
        
        if (position.Closed) 
            return TradeExecutionResult.Success(shortId, "Position is already closed.");
        
        try
        {
            await tpOnlyPositionService.CloseAsync(position, ct);
            
            position.MarkClosed(reason, clock.UtcNow);
           
            await positionStore.SaveAsync(position, ct);
            
            return TradeExecutionResult.Success(shortId, reason);
        }
        catch (Exception ex)
        {
            return TradeExecutionResult.Failure($"{BotName} failed to close '{shortId}': {ex.Message}",  ex);
        }
    }
}
