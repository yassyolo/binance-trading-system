namespace TradingSystem.Backtesting.Strategies;

public sealed class BacktestStrategyRegistry(IEnumerable<IBacktestStrategyFactory> factories)
{
    private readonly IReadOnlyDictionary<string, IBacktestStrategyFactory> _factories = factories
        .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Names => _factories.Keys.OrderBy(x => x).ToArray();

    public IBacktestStrategy Create(string name, IReadOnlyDictionary<string, string> parameters)
    {
        if (!_factories.TryGetValue(name, out var factory))
            throw new InvalidOperationException($"Unknown strategy '{name}'. Available: {string.Join(", ", Names)}");

        return factory.Create(parameters);
    }
}
