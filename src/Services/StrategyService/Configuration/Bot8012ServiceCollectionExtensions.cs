using Microsoft.Extensions.Options;
using StrategyService.Execution;
using StrategyService.Orders;
using StrategyService.Positions;
using StrategyService.Services;
using StrategyService.Services.Healing;
using StrategyService.Strategies.Bot8012;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Observability.Abstractions;
using TradingSystem.Observability.Services;
using TradingSystem.Observability.Storage;

namespace StrategyService.Configuration;

public static class Bot8012ServiceCollectionExtensions
{
    public static IServiceCollection AddBot8012(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<Bot8012Options>()
            .Bind(configuration.GetSection(Bot8012Options.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<Bot8012Options>,
            Bot8012OptionsValidator>();

        services.AddSingleton<Bot8012GapPolicy>();

        services.AddSingleton<
            ITradingStrategy,
            Bot8012Strategy>();

        services.AddSingleton<
            IBotActivePositionProvider,
            Bot8012ActivePositionProvider>();

        services.AddSingleton<
            IBotTradeExecutor,
            Bot8012TradeExecutor>();

        services.AddSingleton<Bot8012PositionEventService>();

        services.AddSingleton<
            IBotOrderEventHandler,
            Bot8012OrderEventHandler>();

        services.AddSingleton<
            IBotHealingService,
            Bot8012HealingService>();

        return services;
    }

    

}

public static class TradingHistoryServiceCollectionExtensions
{
    public static IServiceCollection AddTradingHistory(this IServiceCollection services, IConfiguration configuration) { var path = configuration["TradingHistory:Directory"] ?? "data/trading-history"; services.AddSingleton<ITradingHistoryStore>(_ => new JsonLinesTradingHistoryStore(path)); services.AddSingleton<TradingHistoryRecorder>(); services.AddSingleton<DashboardHistoryQueryService>(); return services; }
}