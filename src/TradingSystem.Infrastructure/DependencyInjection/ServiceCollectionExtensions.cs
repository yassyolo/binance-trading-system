using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Application.Time;
using TradingSystem.Infrastructure.Time;

namespace TradingSystem.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock,  SystemClock>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
