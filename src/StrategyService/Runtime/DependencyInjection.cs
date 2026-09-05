using Microsoft.Extensions.Options;
using StrategyService.Runtime.Configuration;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Runtime;
using TradingSystem.BotRuntime.Runtime.Contracts;

namespace StrategyService.Runtime;

public static class DependencyInjection
{
    public static IServiceCollection AddBotRuntimeOrchestration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BotRuntimeOptions>().Bind(configuration.GetSection(BotRuntimeOptions.SectionName)).ValidateOnStart();   
        services.AddSingleton<IValidateOptions<BotRuntimeOptions>, BotRuntimeOptionsValidator>();
        
        services.AddMemoryCache();
        
        services.AddSingleton<IBotRuntimeStateProvider, CachedBotRuntimeStateProvider>();
        services.AddSingleton<IBotRuntimeConfigurationProvider, CachedBotRuntimeConfigurationProvider>();
       
        services.AddHostedService<BotCommandWorker>();
        services.AddHostedService<BotConfigurationRefreshWorker>();
        
        return services;
    }
}
