using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TradingSystem.PortfolioManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddPortfolioManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PortfolioOptions>()
            .Bind(
                configuration.GetSection(
                    PortfolioOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<PortfolioOptions>,
            PortfolioOptionsValidator>();

        services.TryAddSingleton<
            IPortfolioPerformanceSource,
            NullPortfolioPerformanceSource>();

        services.TryAddSingleton<
            IPaperPortfolioPositionSource,
            NullPaperPortfolioPositionSource>();

        services.AddSingleton<
            IPortfolioSnapshotProvider,
            PortfolioSnapshotProvider>();

        return services;
    }
}