namespace TradingSystem.Binance.Orders;

public sealed class BinanceOrderResult
{
    public required string Symbol { get; init; }
    public required string ClientOrderId { get; init; }
    public required string OrderId { get; init; }
    public string? Status { get; init; }
    public decimal? AveragePrice { get; init; }
}