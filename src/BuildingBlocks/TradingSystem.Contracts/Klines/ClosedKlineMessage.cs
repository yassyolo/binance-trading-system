namespace TradingSystem.Contracts.Klines;

public sealed record ClosedKlineMessage(
    string Symbol,
    long Time,
    string? Open,
    string? High,
    string? Low,
    string? Close,
    string? Volume,
    long CloseTime,
    string Interval);