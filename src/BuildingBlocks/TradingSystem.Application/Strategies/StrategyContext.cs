using TradingSystem.Application.Positions;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Strategies;

public sealed record StrategyContext
{
    public required TradeSignal Signal { get; init; }
    public required decimal MarkPrice { get; init; }
    public required IReadOnlyCollection<ActivePositionView> ActivePositions { get; init; }
    public required DateTime EvaluatedAtUtc { get; init; }
}
