using Microsoft.Extensions.Options;
using StrategyService.Execution;
using StrategyService.Orders;
using StrategyService.Positions;
using StrategyService.Services;
using StrategyService.Strategies.Bot8014;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;

namespace StrategyService.Configuration;

public static class Bot8014ServiceCollectionExtensions
{
    public static IServiceCollection AddBot8014(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<Bot8014Options>()
            .Bind(configuration.GetSection(Bot8014Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<Bot8014Options>,
            Bot8014OptionsValidator>();

        services.AddSingleton<Bot8014GapPolicy>();
        services.AddSingleton<ITradingStrategy, Bot8014Strategy>();

        services.AddSingleton<
            IBotActivePositionProvider,
            Bot8014ActivePositionProvider>();

        services.AddSingleton<
            IBotTradeExecutor,
            Bot8014TradeExecutor>();

        services.AddSingleton<Bot8014PositionEventService>();
        services.AddSingleton<
            IBotOrderEventHandler,
            Bot8014OrderEventHandler>();

        services.AddHostedService<Bot8014HealingService>();

        return services;
    }
}
