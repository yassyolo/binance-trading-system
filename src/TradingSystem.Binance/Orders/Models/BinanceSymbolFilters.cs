namespace TradingSystem.Binance.Orders.Models;

public sealed record BinanceSymbolFilters
{
    public decimal TickSize { get; init; }
    public decimal StepSize { get; init; }
    public decimal MinQuantity { get; init; }
}
