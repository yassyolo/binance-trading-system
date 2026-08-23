namespace TradingSystem.Binance.Orders.Models;

public sealed record BinanceTradeFill
{
    public required string Symbol { get; init; }
    public required string OrderId { get; init; }
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public decimal QuoteQuantity { get; init; }
}
