using TradingSystem.Application.Engine;
using TradingSystem.Application.Strategies;

namespace TradingSystem.Application.Positions;

public sealed class CompositeActivePositionProvider : IActivePositionProvider
{
    private readonly IReadOnlyDictionary<string, IBotActivePositionProvider> _providers;

    public CompositeActivePositionProvider(IEnumerable<IBotActivePositionProvider> providers)
    {
        _providers = providers.ToDictionary(
            x => x.BotName,
            x => x,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string botName,
        string symbol,
        CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(botName, out var provider))
            return Task.FromResult<IReadOnlyCollection<ActivePositionView>>([]);

        return provider.GetActivePositionsAsync(symbol, cancellationToken);
    }
}