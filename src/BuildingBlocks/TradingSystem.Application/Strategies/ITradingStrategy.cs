namespace TradingSystem.Application.Strategies;

public interface ITradingStrategy
{
    string BotName { get; }

    Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken);
}