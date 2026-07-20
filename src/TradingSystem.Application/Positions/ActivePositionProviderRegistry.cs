namespace TradingSystem.Application.Positions;

public sealed class ActivePositionProviderRegistry : IActivePositionProvider
{
    private readonly IReadOnlyDictionary<string,  IBotActivePositionProvider> _providers;

    public ActivePositionProviderRegistry(IEnumerable<IBotActivePositionProvider> providers)
    {
        var map  =  new Dictionary<string,  IBotActivePositionProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            if (!map.TryAdd(provider.BotName,  provider))
                throw new InvalidOperationException($"Multiple active position providers are registered for bot '{provider.BotName}'.");
        }
        _providers  =  map;
    }

    public Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(string botName,  string symbol,  CancellationToken cancellationToken)
         =>  _providers.TryGetValue(botName,  out var provider)
            ? provider.GetActivePositionsAsync(symbol,  cancellationToken)
            : throw new InvalidOperationException($"Active position provider is not registered for bot '{botName}'.");
}
