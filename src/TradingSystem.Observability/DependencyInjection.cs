using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingSystem.Observability.Configuration;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.Pipeline;

namespace TradingSystem.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingObservability(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.AddOptions<TradingEnvironmentOptions>().Validate(o => !string.IsNullOrWhiteSpace(o.EnvironmentName), "Trading environment name is required.").ValidateOnStart();
        }
        else
        {
            services.AddOptions<TradingEnvironmentOptions>();
        }

        services.TryAddSingleton<ITradingEnvironmentProvider, TradingEnvironmentProvider>();
        services.TryAddSingleton<ITradingPipelineRecorder, NullTradingPipelineRecorder>();

        return services;
    }
}