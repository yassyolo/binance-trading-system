using TradingSystem.Backtesting.Bots.Bot8011.Models;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Bots.Optimization;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Bot8011;

public sealed class Bot8011Optimizer(Bot8011BacktestEngine engine,  GridSearchOptimizer optimizer)
{
    public Task<IReadOnlyList<OptimizationResult<Bot8011BacktestOptions,  BotBacktestMetrics>>> RunAsync(
        decimal initialBalance,  
        Bot8011BacktestOptions baseline,  
        Bot8011OptimizationSpace space, 
        IReadOnlyList<MarketCandle> candles,  
        IReadOnlyList<HistoricalBotSignal> signals, 
        int top = 50,  
        CancellationToken ct = default)
    {
        var candidates  = 
            from tp in space.TakeProfitPercents
            from sl in space.StopLossDistances
            from offset in space.Stop3EntryOffsets
            from step in space.Stop3TrailingSteps
            from buffer in space.Stop3TrailingBuffers
            from cooldown in space.Cooldowns
            select baseline with 
            { 
                TakeProfitPercent = tp, 
                InitialStopLossDistance = sl, 
                Stop3EntryOffset = offset, 
                Stop3TrailingStep = step, 
                Stop3TrailingBuffer = buffer, 
                CooldownSeconds = cooldown 
            };
        
        return optimizer.RunAsync(
            candidates, 
            (o,  ct)  =>  engine.RunAsync(initialBalance,  o,  candles,  signals,  ct), 
            x  =>  x.Metrics, 
            m  =>  m.ReturnPercent + Math.Min(m.ProfitFactor,  5m) * 5m - m.MaximumDrawdownPercent * 1.5m - Math.Max(0,  10 - m.ClosedPositions) * 2m, 
            top, 
            ct);
    }
}
