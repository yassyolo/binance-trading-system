using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Engine.Configuration;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Healing;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Risk;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Strategies;

namespace TradingSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TradingEngineOptions>().Bind(configuration.GetSection(TradingEngineOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<TradingEngineOptions>, TradingEngineOptionsValidator>();

        services.AddSingleton<TradingStrategyRegistry>();
        services.AddSingleton<TradeExecutorRegistry>();
        services.AddSingleton<ITradeExecutor>(sp => sp.GetRequiredService<TradeExecutorRegistry>());
        services.AddSingleton<ActivePositionProviderRegistry>();
        services.AddSingleton<IActivePositionProvider>(sp => sp.GetRequiredService<ActivePositionProviderRegistry>());
        services.AddSingleton<HealingServiceRegistry>();
        services.TryAddSingleton<ITradingEngineNotifier, NullTradingEngineNotifier>();
        services.TryAddSingleton<ICentralRiskManager, NullCentralRiskManager>();
        services.AddSingleton<TradingEngine>();
       
        return services;
    }
}