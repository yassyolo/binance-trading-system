namespace TradingSystem.Application.Strategies.Contracts;

public interface ITradingStrategyResolver
{
    Task<ITradingStrategy> ResolveAsync(string botName, CancellationToken ct);
}
