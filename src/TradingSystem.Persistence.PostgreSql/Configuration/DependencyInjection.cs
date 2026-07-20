using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingSystem.Analytics.Abstractions;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Runtime;
using TradingSystem.Persistence.PostgreSql.BotRuntime;
using TradingSystem.Observability.History;
using TradingSystem.Persistence.PostgreSql.Analytics;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.Persistence.PostgreSql.TradingHistory;
using TradingSystem.Persistence.PostgreSql.Reliability;
using TradingSystem.Reconciliation;
using TradingSystem.RiskManagement;
using TradingSystem.Operations;
using TradingSystem.Persistence.PostgreSql.Operations;
using TradingSystem.PaperTrading;
using TradingSystem.Persistence.PostgreSql.PaperTrading;
using TradingSystem.EventStore;
using TradingSystem.Persistence.PostgreSql.EventStore;
using TradingSystem.ReplayEngine;
using TradingSystem.Persistence.PostgreSql.Replay;

namespace TradingSystem.Persistence.PostgreSql.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresTradingHistory(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var options  =  configuration
            .GetSection(PostgresTradingHistoryOptions.SectionName)
            .Get<PostgresTradingHistoryOptions>() ?? new();

        var connectionString  =  configuration.GetConnectionString(options.ConnectionStringName)
            ?? throw new InvalidOperationException($"Missing connection string '{options.ConnectionStringName}'.");

        services.AddSingleton<ITradingDbConnectionFactory>(
            new NpgsqlTradingDbConnectionFactory(
                connectionString, 
                Math.Max(1,  options.CommandTimeoutSeconds)));

        services.RemoveAll<ITradingPipelineRecorder>();
        services.AddSingleton<ITradingPipelineRecorder,  PostgresTradingPipelineRecorder>();
        services.AddSingleton<ITradingHistoryQueryService,  TradingHistoryQueryService>();
        services.AddSingleton<IPerformanceAnalyticsStore,  PostgresPerformanceAnalyticsStore>();
        services.AddSingleton<IReconciliationFindingStore,  PostgresReconciliationFindingStore>();
        services.AddSingleton<IRiskStateProvider,  PostgresRiskStateProvider>();
        services.AddSingleton<IBotRuntimeStateStore,  PostgresBotRuntimeStateStore>();
        services.AddSingleton<IBotRuntimeConfigurationStore,  PostgresBotRuntimeConfigurationStore>();
        services.AddSingleton<IBotCommandQueue,  PostgresBotCommandQueue>();
        services.AddSingleton<PostgresOperationalStore>();
        services.AddSingleton<IServiceHeartbeatStore>(s => s.GetRequiredService<PostgresOperationalStore>());
        services.AddSingleton<IAlertStore>(s => s.GetRequiredService<PostgresOperationalStore>());
        services.AddSingleton<IAuditLog>(s => s.GetRequiredService<PostgresOperationalStore>());
        services.AddSingleton<IAlertCandidateSource, OperationalAlertCandidateSource>();
        services.AddSingleton<IPaperTradingStore,  PostgresPaperTradingStore>();
        services.AddSingleton<ITradingEventStore,  PostgresTradingEventStore>();
        services.AddSingleton<ITradingTimelineReader,  TradingTimelineReader>();
        services.AddSingleton<PostgresReplayStore>();
        services.AddSingleton<IReplayJobStore>(s  =>  s.GetRequiredService<PostgresReplayStore>());
        services.AddSingleton<IReplayEventSource>(s  =>  s.GetRequiredService<PostgresReplayStore>());
        services.AddSingleton<IReplayStrategyEvaluator,  RecordedStrategyEvaluator>();
        services.AddSingleton<TradingSystem.ReplayEngine.ReplayEngine>();

        return services;
    }
}
