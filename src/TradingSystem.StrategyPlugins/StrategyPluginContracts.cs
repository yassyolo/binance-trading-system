using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Application.Strategies;

namespace TradingSystem.StrategyPlugins;

public sealed record StrategyPluginDescriptor(
    string PluginId, 
    string DisplayName, 
    string Version, 
    string StrategyName, 
    string AssemblyName, 
    bool IsBuiltIn, 
    IReadOnlyCollection<string> SupportedSymbols, 
    string? Description  =  null);

public interface IStrategyPluginModule
{
    StrategyPluginDescriptor Descriptor {  get;  }
    void ConfigureServices(IServiceCollection services,  IConfiguration configuration);
}

public interface IStrategyPluginCatalog
{
    IReadOnlyCollection<StrategyPluginDescriptor> GetAll();
    StrategyPluginDescriptor GetRequired(string pluginId);
}

