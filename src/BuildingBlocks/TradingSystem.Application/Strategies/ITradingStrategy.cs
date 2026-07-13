namespace TradingSystem.Application.Strategies;

public interface ITradingStrategy
{
    StrategyMetadata Metadata { get; }

    Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken);
}
