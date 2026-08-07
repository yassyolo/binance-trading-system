using TradingSystem.Backtesting.Bots.Bot8012.Models;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Bots.Optimization;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Bot8012;

public sealed class Bot8012Optimizer(Bot8012BacktestEngine engine,  GridSearchOptimizer optimizer)
{
    public Task<IReadOnlyList<OptimizationResult<Bot8012BacktestOptions, BotBacktestMetrics>>> RunAsync(
        Bot8012BacktestOptions baseline,  
        Bot8012OptimizationSpace space, 
        IReadOnlyList<MarketCandle> candles,  
        IReadOnlyList<HistoricalBotSignal> signals, 
        int top = 50,  
        CancellationToken ct = default)
    {
        var candidates =
            from profit in space.ProfitDistances
            from gap in space.PriceDistances
            from limit in space.SideLimits
            from cooldown in space.Cooldowns
            select baseline with 
            { 
                ProfitDistance = profit,  
                PriceDistance = gap,  
                OrderSideLimit = limit, 
                CooldownSeconds = cooldown 
            };
       
        return optimizer.RunAsync(
            candidates, 
            (o,  _)  =>  Task.FromResult(engine.Run(candles,  signals,  o)), 
            x  =>  x.Metrics, 
            m  =>  m.ReturnPercent + Math.Min(m.ProfitFactor,  5m) * 3m - m.MaximumDrawdownPercent * 2m, 
            top, 
            ct);
    }
}
