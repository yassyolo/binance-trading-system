using TradingSystem.Application.Positions.Models;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Risk.Models;

public sealed record RiskEvaluationContext
{
    public required TradeSignal Signal { get; init; }
    
    public required decimal MarkPrice { get; init; }
    
    public required IReadOnlyCollection<ActivePositionView> ActivePositions { get; init; }
    
    public required DateTime EvaluatedAtUtc { get; init; }
}