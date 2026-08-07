using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;

namespace TradingSystem.Application.Strategies;

public sealed class TradingStrategyRegistry
{
    private readonly IReadOnlyDictionary<string,  ITradingStrategy> _strategies;

    public TradingStrategyRegistry(IEnumerable<ITradingStrategy> strategies)
    {
        var map  =  new Dictionary<string,  ITradingStrategy>(StringComparer.OrdinalIgnoreCase);
        foreach (var strategy in strategies)
        {
            RegisterKey(map,  strategy.Metadata.Name,  strategy);
            RegisterKey(map,  strategy.Metadata.EffectivePluginId,  strategy);
        }
        _strategies  =  map;
    }

    public ITradingStrategy GetRequired(string nameOrPluginId)
         =>  TryGet(nameOrPluginId, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"Trading strategy or plugin '{nameOrPluginId}' is not registered.");

    public bool TryGet(string nameOrPluginId, out ITradingStrategy strategy)
         => _strategies.TryGetValue(nameOrPluginId, out strategy!);

    public IReadOnlyCollection<StrategyMetadata> GetAllMetadata()
         => _strategies.Values.Distinct().Select(x => x.Metadata).ToArray();

    private static void RegisterKey(IDictionary<string, ITradingStrategy> map, string key, ITradingStrategy strategy)
    {
        if (map.TryGetValue(key,  out var existing)  &&  !ReferenceEquals(existing,  strategy))
            throw new InvalidOperationException($"Multiple strategies are registered with key '{key}'.");
       
        map[key]  =  strategy;
    }
}
