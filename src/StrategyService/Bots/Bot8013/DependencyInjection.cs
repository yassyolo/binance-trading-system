using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Healing;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Binance.Startup;
using TradingSystem.Strategies.Grid;

namespace StrategyService.Bots.Bot8013;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8013(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<Bot8013Options>()
            .Bind(configuration.GetSection(Bot8013Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<Bot8013Options>, Bot8013OptionsValidator>();
        services.AddSingleton<IBinanceTradingConfiguration>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<Bot8013Options>>().Value);

        services.AddSingleton(serviceProvider =>
            new TpOnlyGridGapPolicy<Bot8013Options>(
                serviceProvider.GetRequiredService<IOptions<Bot8013Options>>().Value,
                serviceProvider.GetRequiredService<GridSpacingPolicy>()));

        services.AddSingleton<ITradingStrategy, Bot8013Strategy>();
        services.AddSingleton<IBotTradeExecutor, Bot8013TradeExecutor>();
        services.AddSingleton<IBotActivePositionProvider, Bot8013ActivePositionProvider>();
        services.AddSingleton<IBotOrderEventHandler, Bot8013OrderEventHandler>();
        services.AddSingleton<IBotHealingService, Bot8013HealingService>();

        return services;
    }
}
