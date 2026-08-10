using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Binance.Startup;

namespace StrategyService.Bots.Bot8016;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8016(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Bot8016Options>()
            .Bind(configuration.GetSection(Bot8016Options.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<Bot8016Options>, Bot8016OptionsValidator>();
        services.AddSingleton<IBinanceTradingConfiguration>(sp => sp.GetRequiredService<IOptions<Bot8016Options>>().Value);
        services.AddSingleton<Bot8016MarketState>();
        services.AddSingleton<Bot8016EntrySignalEvaluator>();
        services.AddSingleton<Bot8016SignalContextStore>();
        services.AddSingleton<Bot8016OrderExecutionService>();
        services.AddSingleton<Bot8016EntryCoordinator>();
        services.AddSingleton<Bot8016PositionLifecycleService>();
        services.AddSingleton<ITradingStrategy, Bot8016Strategy>();
        services.AddSingleton<IBotTradeExecutor, Bot8016TradeExecutor>();
        services.AddSingleton<IBotActivePositionProvider, Bot8016ActivePositionProvider>();
        services.AddSingleton<IBotOrderEventHandler, Bot8016OrderEventHandler>();
        services.AddHostedService<Bot8016RedisMarketSubscriber>();
        return services;
    }
}
