using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Strategies;

public sealed record BacktestStrategyContext
{
    public required HistoricalCandle CurrentCandle { get; init; }
    public required HistoricalCandle? PreviousCandle { get; init; }
    public required IReadOnlyList<HistoricalCandle> History { get; init; }
    public required BacktestPosition? ActivePosition { get; init; }
    public required decimal Balance { get; init; }
    public required int BarIndex { get; init; }
}
