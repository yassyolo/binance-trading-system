namespace TradingSystem.Binance.Orders.Models;

public sealed record BinanceAlgoOrderResult
{
    public required string Symbol { get; init; }
    public required string ClientOrderId { get; init; }
    public required string AlgoOrderId { get; init; }
    public string? Status { get; init; }
}
