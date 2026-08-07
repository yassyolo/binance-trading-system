namespace TradingSystem.StrategyPlugins.Catalog;

public interface IStrategyPluginCatalog
{
    IReadOnlyCollection<StrategyPluginDescriptor> GetAll();
    StrategyPluginDescriptor GetRequired(string pluginId);
}

