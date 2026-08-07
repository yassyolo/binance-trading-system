namespace TradingSystem.Binance.Orders.Models;

public sealed record BinanceOpenAlgoOrder
{
    public string Symbol { get; init; } = string.Empty;
    public string AlgoOrderId { get; init; } = string.Empty;
    public string ClientAlgoId { get; init; } = string.Empty;
    public string PositionSide { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TriggerPrice { get; init; }
    public decimal Quantity { get; init; }
    public string OrderType { get; init; } = string.Empty;
}
