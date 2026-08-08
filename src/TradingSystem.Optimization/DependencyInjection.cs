using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Optimization.Engine;
using TradingSystem.Optimization.Scoring;

namespace TradingSystem.Optimization;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingOptimization(this IServiceCollection services)
    {
        services.AddSingleton<PerformanceScoreCalculator>();
        services.AddSingleton<ParameterTuningEngine>();
        services.AddSingleton<WalkForwardOptimizationEngine>();
        
        return services;
    }
}
