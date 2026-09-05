using TradingSystem.StrategyPlugins.Models;

namespace TradingSystem.StrategyPlugins.Catalog;

public interface IStrategyPluginCatalog
{    
    StrategyPluginDescriptor GetRequired(string pluginId);
}

