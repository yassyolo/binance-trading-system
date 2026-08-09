using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8015.Configuration;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Binance.Startup;

namespace StrategyService.Bots.Bot8015;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8015(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Bot8015Options>()
            .Bind(configuration.GetSection(Bot8015Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<Bot8015Options>, Bot8015OptionsValidator>();
        services.AddSingleton<IBinanceTradingConfiguration>(serviceProvider => serviceProvider.GetRequiredService<IOptions<Bot8015Options>>().Value);

        services.AddSingleton<ITradingStrategy, Bot8015Strategy>();
        services.AddSingleton<IBotTradeExecutor, Bot8015TradeExecutor>();
        services.AddSingleton<IBotActivePositionProvider, Bot8015ActivePositionProvider>();
        services.AddSingleton<Bot8015PositionEvents>();
        services.AddSingleton<IBotOrderEventHandler>(serviceProvider => serviceProvider.GetRequiredService<Bot8015PositionEvents>());
        services.AddSingleton<IBotHealingService, Bot8015HealingService>();

        services.AddSingleton<Bot8015Stop3OrderService>();
        services.AddSingleton<Bot8015TrailingPriceCache>();
        services.AddHostedService<Bot8015KlineSubscriber>();
        services.AddHostedService<Bot8015TrailingWorker>();

        return services;
    }
}
