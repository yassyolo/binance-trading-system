using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8014.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Binance.Startup;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Strategies.Grid;

namespace StrategyService.Bots.Bot8014;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8014(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Bot8014Options>()
            .Bind(configuration.GetSection(Bot8014Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<Bot8014Options>, Bot8014OptionsValidator>();
        services.AddSingleton<IBinanceTradingConfiguration>(
            serviceProvider => serviceProvider.GetRequiredService<IOptions<Bot8014Options>>().Value);

        services.AddSingleton(serviceProvider =>
            new TpOnlyGridGapPolicy<Bot8014Options>(
                serviceProvider.GetRequiredService<IOptions<Bot8014Options>>().Value,
                serviceProvider.GetRequiredService<GridSpacingPolicy>()));

        services.AddSingleton<ITradingStrategy, Bot8014Strategy>();
        services.AddSingleton<IBotTradeExecutor, Bot8014TradeExecutor>();
        services.AddSingleton<IBotActivePositionProvider, Bot8014ActivePositionProvider>();
        services.AddSingleton<IBotOrderEventHandler, Bot8014OrderEventHandler>();
        services.AddSingleton<IBotHealingService, Bot8014HealingService>();
        services.AddSingleton<ITradingSignalGenerator, Bot8014SignalGenerator>();

        return services;
    }
}
