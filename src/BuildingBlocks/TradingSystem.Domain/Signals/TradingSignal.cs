namespace TradingSystem.Domain.Signals;

public sealed class TradingSignal
{
    public required string Action { get; init; }
    public required string Symbol { get; init; }
    public string Source { get; init; } = "bot8011";
    public DateTime ReceivedAtUtc { get; init; } = DateTime.UtcNow;
}