using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Analytics.Services;

namespace TradingSystem.Analytics;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingAnalytics(this IServiceCollection services)
    {
        services.AddSingleton<PerformanceAnalyticsService>();
        
        return services;
    }
}
