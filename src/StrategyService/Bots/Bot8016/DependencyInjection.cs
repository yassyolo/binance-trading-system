using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Application.Orders;
using TradingSystem.Binance.Startup;

namespace StrategyService.Bots.Bot8016;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8016(this IServiceCollection s, IConfiguration c)
    {
        s.AddOptions<Bot8016Options>()
            .Bind(c.GetSection(Bot8016Options.SectionName))
            .ValidateOnStart();
        
        s.AddSingleton<IValidateOptions<Bot8016Options>, Bot8016OptionsValidator>();
        s.AddSingleton<IBinanceTradingConfiguration>(sp => sp.GetRequiredService<IOptions<Bot8016Options>>().Value);
        s.AddSingleton<Bot8016MarketState>();
        s.AddSingleton<Bot8016EntrySignalEvaluator>();
        s.AddSingleton<Bot8016OrderExecutionService>();
        s.AddSingleton<Bot8016EntryCoordinator>();
        s.AddSingleton<Bot8016PositionLifecycleService>();
        s.AddSingleton<IBotOrderEventHandler, Bot8016OrderEventHandler>();
        s.AddHostedService<Bot8016RedisMarketSubscriber>();
        
        return s;
    }
}
