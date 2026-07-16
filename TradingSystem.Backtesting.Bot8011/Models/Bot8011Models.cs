using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Bot8011.Models;

public sealed record Bot8011Signal(DateTime TimeUtc, TradeSide Side, string Source = "historical-signal");

public enum Bot8011LifecycleStage { InitialProtection, Stop3Active, Closed }

public sealed class Bot8011SimulatedPosition
{
    public required string Id { get; init; }
    public required TradeSide Side { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal InitialQuantity { get; init; }
    public required decimal RemainingQuantity { get; set; }
    public required decimal InitialStopLoss { get; init; }
    public required decimal TakeProfit { get; init; }
    public decimal? Stop3Current { get; set; }
    public Bot8011LifecycleStage Stage { get; set; } = Bot8011LifecycleStage.InitialProtection;
    public decimal RealizedGrossPnl { get; set; }
    public decimal Fees { get; set; }
    public decimal MarginUsed { get; init; }
    public bool TpExecuted { get; set; }
}

public sealed record Bot8011Execution(
    string PositionId, DateTime TimeUtc, string Type, TradeSide Side,
    decimal Price, decimal Quantity, decimal GrossPnl, decimal Fee, string Reason);

public sealed record Bot8011PositionResult
{
    public required string PositionId { get; init; }
    public required TradeSide Side { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required decimal EntryPrice { get; init; }
    public required DateTime ExitTimeUtc { get; init; }
    public required decimal ExitPrice { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal GrossPnl { get; init; }
    public required decimal Fees { get; init; }
    public required decimal NetPnl { get; init; }
    public required string ExitReason { get; init; }
    public required bool TpExecuted { get; init; }
}

public sealed record Bot8011EquityPoint(DateTime TimeUtc, decimal Balance, decimal Peak, decimal Drawdown, decimal DrawdownPercent);

public sealed record Bot8011Metrics
{
    public int Positions { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public decimal WinRatePercent { get; init; }
    public decimal InitialBalance { get; init; }
    public decimal FinalBalance { get; init; }
    public decimal NetProfit { get; init; }
    public decimal ReturnPercent { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal GrossLoss { get; init; }
    public decimal ProfitFactor { get; init; }
    public decimal MaximumDrawdown { get; init; }
    public decimal MaximumDrawdownPercent { get; init; }
    public decimal TotalFees { get; init; }
    public decimal Expectancy { get; init; }
    public int TpReachedCount { get; init; }
}

public sealed record Bot8011BacktestResult
{
    public required string RunId { get; init; }
    public required Bot8011BacktestOptions Options { get; init; }
    public required Bot8011Metrics Metrics { get; init; }
    public required IReadOnlyList<Bot8011PositionResult> Positions { get; init; }
    public required IReadOnlyList<Bot8011Execution> Executions { get; init; }
    public required IReadOnlyList<Bot8011EquityPoint> EquityCurve { get; init; }
    public required IReadOnlyList<HistoricalCandle> Candles { get; init; }
    public required IReadOnlyList<Bot8011Signal> Signals { get; init; }
}
