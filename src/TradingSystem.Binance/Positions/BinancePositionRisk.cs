namespace TradingSystem.Binance.Positions;

public sealed record BinancePositionRisk
{
    public string Symbol { get; init; } = string.Empty;
    public string PositionSide { get; init; } = string.Empty;
    public decimal PositionAmount { get; init; }
    public decimal EntryPrice { get; init; }
    public decimal MarkPrice { get; init; }
    public bool IsOpen => PositionAmount != 0;
}
