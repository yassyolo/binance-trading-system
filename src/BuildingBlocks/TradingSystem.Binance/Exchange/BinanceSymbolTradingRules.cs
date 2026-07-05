namespace TradingSystem.Binance.Exchange;

public sealed record BinanceSymbolTradingRules(
    decimal TickSize,
    decimal StepSize,
    decimal MinQuantity);