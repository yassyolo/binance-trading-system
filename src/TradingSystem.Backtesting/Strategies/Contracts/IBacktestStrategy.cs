using TradingSystem.Backtesting.Strategies.Models;

namespace TradingSystem.Backtesting.Strategies.Contracts;

public interface IBacktestStrategy
{
    string Name { get; }
    int WarmupBars { get; }
    ValueTask<StrategyDecision> DecideAsync(BacktestStrategyContext context,  CancellationToken ct);
}
