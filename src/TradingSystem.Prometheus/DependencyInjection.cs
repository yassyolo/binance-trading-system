using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TradingSystem.Prometheus;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingPrometheus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PrometheusOptions>()
            .Bind(configuration.GetSection(PrometheusOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<PrometheusOptions>, PrometheusOptionsValidator>();
        services.AddSingleton<TradingMetrics>();
        services.AddHostedService<PrometheusMetricServerWorker>();
        services.AddHostedService<PortfolioMetricsWorker>();
        return services;
    }
}