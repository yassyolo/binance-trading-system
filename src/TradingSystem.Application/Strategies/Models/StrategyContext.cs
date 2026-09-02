using TradingSystem.Domain.Signals;
using TradingSystem.Application.Positions.Models;
using TradingSystem.BotRuntime.Configuration.Models;

namespace TradingSystem.Application.Strategies.Models;

public sealed record StrategyContext
{
    public required TradeSignal Signal { get; init; }
    
    public required decimal MarkPrice { get; init; }
    
    public required IReadOnlyCollection<ActivePositionView> ActivePositions { get; init; }
    
    public required DateTime EvaluatedAtUtc { get; init; }
   
    public BotRuntimeConfiguration? RuntimeConfiguration { get; init; }
}
