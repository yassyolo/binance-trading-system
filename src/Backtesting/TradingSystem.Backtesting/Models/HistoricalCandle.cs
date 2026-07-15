namespace TradingSystem.Backtesting.Models;

public sealed record HistoricalCandle
{
    public required string Symbol { get; init; }
    public required string Interval { get; init; }
    public required DateTime OpenTimeUtc { get; init; }
    public required DateTime CloseTimeUtc { get; init; }
    public required decimal Open { get; init; }
    public required decimal High { get; init; }
    public required decimal Low { get; init; }
    public required decimal Close { get; init; }
    public required decimal Volume { get; init; }
}
