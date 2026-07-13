using Microsoft.Extensions.Options;
using StrategyService.Execution;
using StrategyService.Market;
using StrategyService.Orders;
using StrategyService.Positions;
using StrategyService.Services;
using StrategyService.Strategies.Bot8015;
using StrategyService.Workers;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;

namespace StrategyService.Configuration;

public static class Bot8015ServiceCollectionExtensions
{
    public static IServiceCollection AddBot8015(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<Bot8015Options>()
            .Bind(configuration.GetSection(Bot8015Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<Bot8015Options>,
            Bot8015OptionsValidator>();

        services.AddSingleton<ITradingStrategy, Bot8015Strategy>();

        services.AddSingleton<
            IBotActivePositionProvider,
            Bot8015ActivePositionProvider>();

        services.AddSingleton<
            IBotTradeExecutor,
            Bot8015TradeExecutor>();

        services.AddSingleton<Bot8015OrderExecutionService>();
        services.AddSingleton<Bot8015Stop3OrderService>();
        services.AddSingleton<Bot8015PositionEventService>();

        services.AddSingleton<
            IBotOrderEventHandler,
            Bot8015OrderEventHandler>();

        services.AddSingleton<Bot8015TrailingPriceCache>();

        services.AddHostedService<Bot8015KlineSubscriber>();
        services.AddHostedService<Bot8015Stop3TrailingWorker>();
        services.AddHostedService<Bot8015HealingService>();

        return services;
    }
}
