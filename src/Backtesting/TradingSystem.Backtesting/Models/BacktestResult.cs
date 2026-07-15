namespace TradingSystem.Backtesting.Models;

public sealed record BacktestResult
{
    public required string RunId { get; init; }
    public required BacktestRequest Request { get; init; }
    public required DateTime StartedAtUtc { get; init; }
    public required DateTime CompletedAtUtc { get; init; }
    public required decimal InitialBalance { get; init; }
    public required decimal FinalBalance { get; init; }
    public required BacktestMetrics Metrics { get; init; }
    public required IReadOnlyList<BacktestTrade> Trades { get; init; }
    public required IReadOnlyList<EquityPoint> EquityCurve { get; init; }
    public required IReadOnlyList<HistoricalCandle> Candles { get; init; }
    public required IReadOnlyList<SignalMarker> Signals { get; init; }
}

public sealed record SignalMarker(DateTime TimeUtc, TradeSide Side, decimal Price, bool Executed, string Reason);
