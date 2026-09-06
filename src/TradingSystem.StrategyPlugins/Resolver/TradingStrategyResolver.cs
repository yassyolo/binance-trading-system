using TradingSystem.Application.Strategies;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace TradingSystem.StrategyPlugins.Resolver;

public sealed class TradingStrategyResolver(
    TradingStrategyRegistry startegyRegistry, 
    IBotRuntimeConfigurationProvider configProvider) 
    : ITradingStrategyResolver
{
    public async Task<ITradingStrategy> ResolveAsync(string botName, CancellationToken ct)
    {
        var config = await configProvider.GetAsync(botName, ct);
        
        if (!string.IsNullOrWhiteSpace(config?.StrategyType) 
            && startegyRegistry.TryGet(config.StrategyType, out var configured))
            return configured;

        return startegyRegistry.GetRequired(botName);
    }
}
