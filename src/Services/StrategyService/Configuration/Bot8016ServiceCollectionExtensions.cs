using Microsoft.Extensions.Options;
using StrategyService.Market;
using StrategyService.Orders;
using StrategyService.Services;
using StrategyService.Strategies.Bot8016;
using TradingSystem.Application.Orders;

namespace StrategyService.Configuration;

public static class Bot8016ServiceCollectionExtensions
{
    public static IServiceCollection AddBot8016(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<Bot8016Options>()
            .Bind(configuration.GetSection(Bot8016Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<Bot8016Options>,
            Bot8016OptionsValidator>();

        services.AddSingleton<Bot8016MarketState>();
        services.AddSingleton<Bot8016EntrySignalEvaluator>();
        services.AddSingleton<Bot8016OrderExecutionService>();
        services.AddSingleton<Bot8016EntryCoordinator>();
        services.AddSingleton<Bot8016PositionLifecycleService>();

        services.AddSingleton<
            IBotOrderEventHandler,
            Bot8016OrderEventHandler>();

        services.AddHostedService<Bot8016RedisMarketSubscriber>();

        return services;
    }
}
