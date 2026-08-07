using TradingSystem.Application.Strategies.Models;

namespace TradingSystem.Application.Strategies.Contracts;

public interface ITradingStrategy
{
    StrategyMetadata Metadata { get; }
    
    Task<StrategyDecision> DecideAsync(StrategyContext context,  CancellationToken ct);
}
