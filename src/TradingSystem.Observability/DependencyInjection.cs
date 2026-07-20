using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History;
namespace TradingSystem.Observability;
public static class DependencyInjection
{
    public static IServiceCollection AddTradingObservability(this IServiceCollection services)
    {
        services.TryAddSingleton<ITradingEnvironmentProvider, TradingEnvironmentProvider>();
        services.TryAddSingleton<ITradingPipelineRecorder, NullTradingPipelineRecorder>();

        return services;
    }
}
