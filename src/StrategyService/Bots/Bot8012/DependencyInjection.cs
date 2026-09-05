using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8012.Configuration;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Binance.Startup;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Strategies.Grid;

namespace StrategyService.Bots.Bot8012;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8012(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<Bot8012Options>().Bind(config.GetSection(Bot8012Options.SectionName)).ValidateOnStart();   
        services.AddSingleton<IValidateOptions<Bot8012Options>, Bot8012OptionsValidator>();
       
        services.AddSingleton<IBinanceTradingConfiguration>(sp => sp.GetRequiredService<IOptions<Bot8012Options>>().Value);
        
        services.AddSingleton(sp => new TpOnlyGridGapPolicy<Bot8012Options>(
            sp.GetRequiredService<IOptions<Bot8012Options>>().Value,
            sp.GetRequiredService<GridSpacingPolicy>())); services.AddSingleton<ITradingStrategy, Bot8012Strategy>();
       
        services.AddSingleton<IBotTradeExecutor, Bot8012TradeExecutor>();
        services.AddSingleton<IBotActivePositionProvider, Bot8012ActivePositionProvider>();
        services.AddSingleton<IBotOrderEventHandler, Bot8012OrderEventHandler>();
        services.AddSingleton<IBotHealingService, Bot8012HealingService>();
        services.AddSingleton<ITradingSignalGenerator, Bot8012SignalGenerator>();
        
        return services;
    }
}
