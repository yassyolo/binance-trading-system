using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Backtesting.Costs;
using TradingSystem.Backtesting.Engine;
using TradingSystem.Backtesting.Execution;
using TradingSystem.Backtesting.Metrics;
using TradingSystem.Backtesting.Risk;
using TradingSystem.Backtesting.Strategies;

namespace TradingSystem.Backtesting;

public static class DependencyInjection
{
    public static IServiceCollection AddBacktesting(this IServiceCollection services)
    {
        services.AddSingleton<PositionSizeCalculator>();
        services.AddSingleton<CandleFillResolver>();
        services.AddSingleton<BacktestMetricsCalculator>();
        services.AddSingleton<ITradingCostModel>(_  =>  new BinanceFuturesCostModel());
        services.AddSingleton<BacktestStrategyRegistry>();
        services.AddSingleton<BacktestEngine>();
        services.AddBacktestStrategy<EmaCrossStrategyFactory>();
        return services;
    }

    public static IServiceCollection AddBacktestStrategy<TFactory>(this IServiceCollection services)
        where TFactory : class,  IBacktestStrategyFactory
    {
        services.AddSingleton<IBacktestStrategyFactory,  TFactory>();
        return services;
    }
}
