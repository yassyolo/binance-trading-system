namespace TradingSystem.Binance.Orders.Models;

public sealed record BinanceOpenOrder
{
    public string Symbol { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public string ClientOrderId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Side { get; init; } = string.Empty;
    public string PositionSide { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdateTimeUtc { get; init; }
}
