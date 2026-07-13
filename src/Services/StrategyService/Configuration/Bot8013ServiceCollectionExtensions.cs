using Microsoft.Extensions.Options;
using StrategyService.Execution;
using StrategyService.Orders;
using StrategyService.Positions;
using StrategyService.Services;
using StrategyService.Strategies.Bot8013;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;

namespace StrategyService.Configuration;

public static class Bot8013ServiceCollectionExtensions
{
    public static IServiceCollection AddBot8013(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<Bot8013Options>()
            .Bind(configuration.GetSection(Bot8013Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<Bot8013Options>,
            Bot8013OptionsValidator>();

        services.AddSingleton<Bot8013GapPolicy>();
        services.AddSingleton<ITradingStrategy, Bot8013Strategy>();

        services.AddSingleton<
            IBotActivePositionProvider,
            Bot8013ActivePositionProvider>();

        services.AddSingleton<
            IBotTradeExecutor,
            Bot8013TradeExecutor>();

        services.AddSingleton<Bot8013PositionEventService>();
        services.AddSingleton<
            IBotOrderEventHandler,
            Bot8013OrderEventHandler>();

        services.AddHostedService<Bot8013HealingService>();

        return services;
    }
}
