using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Binance.Startup;

namespace StrategyService.Bots.Bot8011;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8011(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Bot8011Options>().Bind(configuration.GetSection(Bot8011Options.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<Bot8011Options>, Bot8011OptionsValidator>();
        
        services.AddSingleton<IBinanceTradingConfiguration>(sp => sp.GetRequiredService<IOptions<Bot8011Options>>().Value);
        services.AddSingleton<ITradingStrategy, Bot8011Strategy>();
        services.AddSingleton<IBotTradeExecutor, Bot8011TradeExecutor>();
        services.AddSingleton<IBotActivePositionProvider, Bot8011ActivePositionProvider>();
        services.AddSingleton<IBotOrderEventHandler, Bot8011PositionEvents>();
        services.AddSingleton<IBotHealingService, Bot8011HealingService>();
        services.AddSingleton<Bot8011Stop3OrderService>();
        services.AddSingleton<Bot8011TrailingPriceCache>();
        
        services.AddHostedService<Bot8011KlineSubscriber>();
        services.AddHostedService<Bot8011TrailingWorker>();
        return services;
    }
}
