using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.StrategyPlugins.Loading;

namespace TradingSystem.StrategyPlugins.Catalog;

public sealed class StrategyPluginCatalog : IStrategyPluginCatalog
{
    private readonly IReadOnlyDictionary<string,  StrategyPluginDescriptor> _plugins;

    public StrategyPluginCatalog(IEnumerable<ITradingStrategy> strategies,  IEnumerable<IStrategyPluginModule> modules)
    {
        var descriptors  =  new List<StrategyPluginDescriptor>();
        descriptors.AddRange(modules.Select(x  =>  x.Descriptor));

        foreach (var strategy in strategies)
        {
            var metadata  =  strategy.Metadata;
            var pluginId  =  string.IsNullOrWhiteSpace(metadata.PluginId) ? metadata.Name : metadata.PluginId;
            if (descriptors.Any(x  =>  x.PluginId.Equals(pluginId,  StringComparison.OrdinalIgnoreCase)))
                continue;

            descriptors.Add(new StrategyPluginDescriptor(
                pluginId, 
                metadata.DisplayName ?? metadata.Name, 
                metadata.Version, 
                metadata.Name, 
                strategy.GetType().Assembly.GetName().Name ?? "unknown", 
                true, 
                metadata.SupportedSymbols, 
                metadata.Description));
        }

        _plugins  =  descriptors
            .GroupBy(x  =>  x.PluginId,  StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x  =>  x.Key, 
                x  =>  x.OrderByDescending(d  =>  ParseVersion(d.Version)).First(), 
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<StrategyPluginDescriptor> GetAll()
         =>  _plugins.Values.OrderBy(x  =>  x.DisplayName).ToArray();

    public StrategyPluginDescriptor GetRequired(string pluginId)
         =>  _plugins.TryGetValue(pluginId,  out var descriptor)
            ? descriptor
            : throw new InvalidOperationException($"Strategy plugin '{pluginId}' is not registered.");

    private static Version ParseVersion(string value)  =>  Version.TryParse(value,  out var version) ? version : new Version(0,  0);
}
