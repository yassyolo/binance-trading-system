namespace TradingSystem.Application.Strategies;

public interface ITradingStrategyResolver
{
    Task<ITradingStrategy> ResolveAsync(string botName,  CancellationToken cancellationToken);
    ITradingStrategy ResolveByPluginId(string pluginId);
}
