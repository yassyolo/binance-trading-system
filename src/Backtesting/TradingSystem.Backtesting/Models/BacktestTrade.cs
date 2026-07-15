namespace TradingSystem.Backtesting.Models;

public sealed record BacktestTrade
{
    public required long Id { get; init; }
    public required string Symbol { get; init; }
    public required TradeSide Side { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required DateTime ExitTimeUtc { get; init; }
    public required decimal EntryPrice { get; init; }
    public required decimal ExitPrice { get; init; }
    public required decimal Quantity { get; init; }
    public required ExitReason ExitReason { get; init; }
    public required decimal GrossPnl { get; init; }
    public required decimal EntryFee { get; init; }
    public required decimal ExitFee { get; init; }
    public required decimal FundingCost { get; init; }
    public required decimal NetPnl { get; init; }
    public decimal RMultiple { get; init; }
}
