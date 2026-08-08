using TradingSystem.Application.Strategies;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace TradingSystem.StrategyPlugins.Resolver;

public sealed class TradingStrategyResolver(
    TradingStrategyRegistry registry, 
    IBotRuntimeConfigurationProvider configurations) 
    : ITradingStrategyResolver
{
    public async Task<ITradingStrategy> ResolveAsync(string botName, CancellationToken ct)
    {
        var config  =  await configurations.GetAsync(botName, ct);
        
        if (!string.IsNullOrWhiteSpace(config?.StrategyType) 
            && registry.TryGet(config.StrategyType, out var configured))
            return configured;

        return registry.GetRequired(botName);
    }

    public ITradingStrategy ResolveByPluginId(string pluginId) 
        => registry.GetRequired(pluginId);
}
