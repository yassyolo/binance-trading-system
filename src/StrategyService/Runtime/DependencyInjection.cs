using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Runtime;

namespace StrategyService.Runtime;

public static class DependencyInjection
{
    public static IServiceCollection AddBotRuntimeOrchestration(this IServiceCollection services,  IConfiguration configuration)
    {
        services.AddOptions<BotRuntimeOptions>().Bind(configuration.GetSection(BotRuntimeOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<IBotRuntimeStateProvider,  CachedBotRuntimeStateProvider>();
        services.AddSingleton<IBotRuntimeConfigurationProvider,  CachedBotRuntimeConfigurationProvider>();
        services.AddHostedService<BotCommandWorker>();
        services.AddHostedService<BotConfigurationRefreshWorker>();
        return services;
    }
}
