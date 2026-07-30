using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting; // Add this using directive

namespace TradingSystem.HistoricalDatabase;

public static class DependencyInjection
{
    public static IServiceCollection AddHistoricalDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<HistoricalDatabaseOptions>()
            .Bind(configuration.GetSection(HistoricalDatabaseOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<HistoricalDatabaseOptions>, HistoricalDatabaseOptionsValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PostgresHistoricalEventStore>();
        services.AddSingleton<IHistoricalEventStore>(sp => sp.GetRequiredService<PostgresHistoricalEventStore>());
        services.AddSingleton<IHistoricalEventSink>(sp => sp.GetRequiredService<PostgresHistoricalEventStore>());
        services.AddHostedService<HistoricalPortfolioSnapshotWorker>();
        services.AddHostedService<HistoricalRetentionWorker>();
        return services;
    }
}