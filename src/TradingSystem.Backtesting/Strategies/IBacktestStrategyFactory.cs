namespace TradingSystem.Backtesting.Strategies;

public interface IBacktestStrategyFactory
{
    string Name {  get;  }
    IBacktestStrategy Create(IReadOnlyDictionary<string,  string> parameters);
}
