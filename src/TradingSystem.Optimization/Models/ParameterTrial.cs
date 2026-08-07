using TradingSystem.Backtesting.Bots.Models;

namespace TradingSystem.Optimization.Models;

public sealed record ParameterTrial<TOptions>
{
    public required int Sequence { get; init; }
    
    public required TOptions Options { get; init; }
    
    public required BotBacktestMetrics Metrics { get; init; }
    
    public required decimal Score { get; init; }
}
