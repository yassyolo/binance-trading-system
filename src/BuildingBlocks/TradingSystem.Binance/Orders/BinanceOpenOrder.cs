namespace TradingSystem.Binance.Orders;

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