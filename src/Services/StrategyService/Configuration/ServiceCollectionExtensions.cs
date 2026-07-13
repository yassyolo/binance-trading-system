using Microsoft.Extensions.DependencyInjection.Extensions;
using StrategyService.Services;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Application.Time;
using TradingSystem.Infrastructure.Time;
using TradingSystem.Redis.Engine;

namespace StrategyService.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IClock, SystemClock>();

        services.AddSingleton<ISignalIdempotencyStore, RedisSignalIdempotencyStore>();
        services.AddSingleton<ISignalCooldownStore, RedisSignalCooldownStore>();
        services.AddSingleton<ITradingOperationLockProvider, RedisTradingOperationLockProvider>();

        services.AddSingleton<ITradingEngineNotifier, TelegramTradingEngineNotifier>();

        services.AddSingleton<CompositeTradingStrategy>();
        services.AddSingleton<IActivePositionProvider, CompositeActivePositionProvider>();
        services.AddSingleton<ITradeExecutor, CompositeTradeExecutor>();

        services.AddSingleton<TradingEngine>();

        return services;
    }
}