using Microsoft.Extensions.Options;
using StrategyService.Execution;
using StrategyService.Market;
using StrategyService.Orders;
using StrategyService.Positions;
using StrategyService.Services;
using StrategyService.Services.Healing;
using StrategyService.Strategies.Bot8011;
using StrategyService.Workers;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;

namespace StrategyService.Configuration;

public static class Bot8011ServiceCollectionExtensions
{
    public static IServiceCollection AddBot8011(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<Bot8011Options>()
            .Bind(configuration.GetSection(Bot8011Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<Bot8011Options>,
            Bot8011OptionsValidator>();

        services.AddSingleton<ITradingStrategy, Bot8011Strategy>();
        services.AddSingleton<IBotTradeExecutor, Bot8011TradeExecutor>();
        services.AddSingleton<IBotActivePositionProvider, Bot8011ActivePositionProvider>();
        services.AddSingleton<IBotOrderEventHandler, Bot8011OrderEventHandler>();
        services.AddSingleton<IBotHealingService, Bot8011HealingService>();

        services.AddSingleton<Bot8011Stop3OrderService>();
        services.AddSingleton<Bot8011PositionEventService>();
        services.AddSingleton<Bot8011TrailingPriceCache>();

        services.AddHostedService<Bot8011KlineSubscriber>();
        services.AddHostedService<Bot8011Stop3TrailingWorker>();

        return services;
    }
}
