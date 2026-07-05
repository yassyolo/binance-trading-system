namespace TradingSystem.Application.Strategies;

public sealed class StrategyContext
{
    public required TradeSignal Signal { get; init; }
    public required decimal MarkPrice { get; init; }
    public required IReadOnlyCollection<ActivePositionView> ActivePositions { get; init; }
}