namespace TradingSystem.Backtesting.Strategies;

public interface IBacktestStrategy
{
    string Name {  get;  }
    int WarmupBars {  get;  }
    ValueTask<StrategyDecision> DecideAsync(BacktestStrategyContext context,  CancellationToken ct);
}
