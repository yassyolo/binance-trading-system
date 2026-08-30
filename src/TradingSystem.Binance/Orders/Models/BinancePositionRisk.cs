namespace TradingSystem.Binance.Orders.Models;

public sealed record BinancePositionRisk
{
    public string Symbol { get; init; } = string.Empty;
    
    public string PositionSide { get; init; } = string.Empty;
    
    public decimal PositionAmount { get; init; }
    
    public decimal EntryPrice { get; init; }
    
    public decimal MarkPrice { get; init; }
}
