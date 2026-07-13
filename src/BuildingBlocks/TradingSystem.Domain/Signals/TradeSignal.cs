using TradingSystem.Domain.Enums;

namespace TradingSystem.Domain.Signals;

public sealed record TradeSignal
{
    public required string SignalId { get; init; }
    public required string BotName { get; init; }
    public required string Symbol { get; init; }
    public required PositionSide Side { get; init; }

    public string? Source { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
}
