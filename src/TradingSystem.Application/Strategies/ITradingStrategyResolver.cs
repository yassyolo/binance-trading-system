namespace TradingSystem.Application.Strategies;

public interface ITradingStrategyResolver
{
    Task<ITradingStrategy> ResolveAsync(string botName,  CancellationToken ct);
    ITradingStrategy ResolveByPluginId(string pluginId);
}
