namespace TradingSystem.Application.Strategies;

public sealed class CompositeTradingStrategy
{
    private readonly IReadOnlyDictionary<string, ITradingStrategy> _strategies;

    public CompositeTradingStrategy(IEnumerable<ITradingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            x => x.Metadata.Name,
            x => x,
            StringComparer.OrdinalIgnoreCase);
    }

    public ITradingStrategy GetRequired(string botName)
    {
        if (!_strategies.TryGetValue(botName, out var strategy))
        {
            throw new InvalidOperationException(
                $"Trading strategy is not registered for bot '{botName}'.");
        }

        return strategy;
    }
}
