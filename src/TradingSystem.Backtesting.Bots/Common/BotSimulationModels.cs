using TradingSystem.Backtesting.Models;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Common;

public sealed record BotExecution(
    string PositionId, 
    DateTime TimeUtc, 
    string Type, 
    TradeSide Side, 
    decimal Price, 
    decimal Quantity, 
    decimal GrossPnl, 
    decimal Fee, 
    string Reason);

public sealed record BotPositionResult
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
    public bool PartialTakeProfitReached { get; init; }
}

public sealed record BotEquityPoint(
    DateTime TimeUtc, 
    decimal Balance, 
    decimal Equity, 
    decimal Peak, 
    decimal DrawdownAmount, 
    decimal DrawdownPercent);

public sealed record BotBacktestMetrics
{
    public int Signals { get; init; }
    public int OpenedPositions { get; init; }
    public int BlockedSignals { get; init; }
    public int ClosedPositions { get; init; }
    public int WinningPositions { get; init; }
    public int LosingPositions { get; init; }
    public decimal WinRatePercent { get; init; }
    public decimal InitialBalance { get; init; }
    public decimal FinalBalance { get; init; }
    public decimal NetProfit { get; init; }
    public decimal ReturnPercent { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal GrossLoss { get; init; }
    public decimal ProfitFactor { get; init; }
    public decimal MaximumDrawdownAmount { get; init; }
    public decimal MaximumDrawdownPercent { get; init; }
    public decimal TotalFees { get; init; }
    public decimal Expectancy { get; init; }
    public int PartialTakeProfits { get; init; }
}

public sealed record BotSignalDecision(
    DateTime TimeUtc, 
    TradeSide Side, 
    string Decision, 
    string Reason, 
    decimal ReferencePrice, 
    string? SignalId);

public sealed record BotBacktestResult<TOptions>
{
    public required string RunId { get; init; }
    public required string BotName { get; init; }
    public required TOptions Options { get; init; }
    public required DateTime StartedAtUtc { get; init; }
    public required DateTime CompletedAtUtc { get; init; }
    public required BotBacktestMetrics Metrics { get; init; }
    public required IReadOnlyList<BotPositionResult> Positions { get; init; }
    public required IReadOnlyList<BotExecution> Executions { get; init; }
    public required IReadOnlyList<BotSignalDecision> Decisions { get; init; }
    public required IReadOnlyList<BotEquityPoint> EquityCurve { get; init; }
    public required IReadOnlyList<MarketCandle> Candles { get; init; }
    public required IReadOnlyList<HistoricalBotSignal> Signals { get; init; }
}
