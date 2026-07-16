using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Persistence.PostgreSql.TradingHistory;
using TradingSystem.Signals.Abstractions;

namespace TradingSystem.Persistence.PostgreSql.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresTradingHistory(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(
            PostgresTradingHistoryOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing connection string '{PostgresTradingHistoryOptions.ConnectionStringName}'.");

        services.AddSingleton<ITradingPipelineRecorder>(
            new PostgresTradingPipelineRecorder(connectionString));
        services.AddSingleton(new TradingHistoryQueryService(connectionString));
        return services;
    }
}
