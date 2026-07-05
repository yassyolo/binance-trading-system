namespace TradingSystem.Binance.Orders;

public sealed class BinanceOrderResult
{
    public required string Symbol { get; init; }
    public required string ClientOrderId { get; init; }
    public required string OrderId { get; init; }

    public string? Status { get; init; }
    public decimal? AveragePrice { get; init; }

    public decimal? ExecutedQuantity { get; init; }
    public decimal? CumulativeQuoteQuantity { get; init; }
}

public sealed record BinanceSymbolFilters
{
    public decimal TickSize { get; init; }
    public decimal StepSize { get; init; }
}