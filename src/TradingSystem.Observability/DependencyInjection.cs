using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.Environment.Configuration;
using TradingSystem.Observability.Pipeline;

namespace TradingSystem.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingObservability(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.AddOptions<TradingEnvironmentOptions>()
                .Bind(
                    configuration.GetSection(
                        TradingEnvironmentOptions.SectionName))
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(
                            options.EnvironmentName),
                    "Trading environment name is required.")
                .ValidateOnStart();
        }
        else
        {
            services.AddOptions<TradingEnvironmentOptions>();
        }

        services.TryAddSingleton<
            ITradingEnvironmentProvider,
            TradingEnvironmentProvider>();

        services.TryAddSingleton<
            ITradingPipelineRecorder,
            NullTradingPipelineRecorder>();

        return services;
    }
}