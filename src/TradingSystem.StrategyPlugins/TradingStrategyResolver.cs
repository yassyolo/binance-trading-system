using TradingSystem.Application.Strategies;
using TradingSystem.BotRuntime.Configuration;

namespace TradingSystem.StrategyPlugins;

public sealed class TradingStrategyResolver(
    TradingStrategyRegistry registry, 
    IBotRuntimeConfigurationProvider configurations) : ITradingStrategyResolver
{
    public async Task<ITradingStrategy> ResolveAsync(string botName,  CancellationToken cancellationToken)
    {
        var configuration  =  await configurations.GetAsync(botName,  cancellationToken);
        if (!string.IsNullOrWhiteSpace(configuration?.StrategyType) && registry.TryGet(configuration.StrategyType,  out var configured))
            return configured;

        return registry.GetRequired(botName);
    }

    public ITradingStrategy ResolveByPluginId(string pluginId)  =>  registry.GetRequired(pluginId);
}
