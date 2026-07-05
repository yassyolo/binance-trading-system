using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Strategies;

public sealed class TradeSignal
{
    public required string BotName { get; init; }
    public required string Symbol { get; init; }
    public required PositionSide Side { get; init; }
    public required string Source { get; init; }
}