using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TradingSystem.StrategyPlugins.Loading;

public interface IStrategyPluginModule
{
    StrategyPluginDescriptor Descriptor { get; }
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}