namespace TradingSystem.Backtesting.Strategies.Contracts;

public interface IBacktestStrategyFactory
{
    string Name { get; }
    
    IBacktestStrategy Create(IReadOnlyDictionary<string, string> parameters);
}
