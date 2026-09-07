using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingSystem.Analytics.Contracts;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Runtime.Contracts;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.TradingTimeline;
using TradingSystem.Observability.Pipeline;
using TradingSystem.Operations.Contracts;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.Persistence.PostgreSql.Analytics;
using TradingSystem.Persistence.PostgreSql.BotRuntime;
using TradingSystem.Persistence.PostgreSql.Configuration;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.Persistence.PostgreSql.EventStore;
using TradingSystem.Persistence.PostgreSql.Operations;
using TradingSystem.Persistence.PostgreSql.PaperTrading;
using TradingSystem.Persistence.PostgreSql.Portfolio;
using TradingSystem.Persistence.PostgreSql.Reliability;
using TradingSystem.Persistence.PostgreSql.Replay;
using TradingSystem.Persistence.PostgreSql.TradingHistory;
using TradingSystem.PortfolioManagement.Performance;
using TradingSystem.PortfolioManagement.Position;
using TradingSystem.Reconciliation.Contracts;
using TradingSystem.ReplayEngine.Contracts;
using TradingSystem.ReplayEngine.Evaluator;
using TradingSystem.ReplayEngine.Store;

namespace TradingSystem.Persistence.PostgreSql;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresTradingHistory(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(PostgresTradingHistoryOptions.SectionName).Get<PostgresTradingHistoryOptions>() ?? new();

        if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
            throw new InvalidOperationException("TradingHistory ConnectionStringName is required.");

        if (options.CommandTimeoutSeconds <= 0)
            throw new InvalidOperationException("TradingHistory CommandTimeoutSeconds must be positive.");

        var connectionString = configuration.GetConnectionString(options.ConnectionStringName)
            ?? throw new InvalidOperationException($"Missing connection string '{options.ConnectionStringName}'.");

        services.AddSingleton<ITradingDbConnectionFactory>(new NpgsqlTradingDbConnectionFactory(connectionString, Math.Max(1, options.CommandTimeoutSeconds)));

        RegisterTradingHistory(services);
        RegisterAnalytics(services);
        RegisterRiskAndReliability(services);
        RegisterBotRuntime(services);
        RegisterOperations(services);
        RegisterPortfolio(services);
        RegisterPaperTrading(services);
        RegisterEventStore(services);
        RegisterReplayEngine(services);

        return services;
    }

    private static void RegisterTradingHistory(IServiceCollection services)
    {
        services.RemoveAll<ITradingPipelineRecorder>();

        services.AddSingleton<ITradingPipelineRecorder, PostgresTradingPipelineRecorder>();
        services.AddSingleton<ITradingHistoryQueryService, TradingHistoryQueryService>();
    }

    private static void RegisterAnalytics(IServiceCollection services)
    {
        services.AddSingleton<IPerformanceAnalyticsStore, PostgresPerformanceAnalyticsStore>();
    }

    private static void RegisterRiskAndReliability(IServiceCollection services)
    {
        services.RemoveAll<IRiskStateProvider>();
        services.AddSingleton<IRiskStateProvider, PostgresRiskStateProvider>();
        services.AddSingleton<IReconciliationFindingStore, PostgresReconciliationFindingStore>();
    }

    private static void RegisterBotRuntime(IServiceCollection services)
    {
        services.AddSingleton<IBotRuntimeStateStore, PostgresBotRuntimeStateStore>();
        services.AddSingleton<IBotRuntimeConfigurationStore, PostgresBotRuntimeConfigurationStore>();
        services.AddSingleton<IBotCommandQueue, PostgresBotCommandQueue>();
    }

    private static void RegisterOperations(IServiceCollection services)
    {
        services.AddSingleton<PostgresOperationalStore>();
        services.AddSingleton<IServiceHeartbeatStore>(sp => sp.GetRequiredService<PostgresOperationalStore>());
        services.AddSingleton<IAlertStore>(sp => sp.GetRequiredService<PostgresOperationalStore>());
        services.AddSingleton<IAuditLog>(sp => sp.GetRequiredService<PostgresOperationalStore>());
        services.AddSingleton<IAlertCandidateSource, OperationalAlertCandidateSource>();
    }

    private static void RegisterPortfolio(IServiceCollection services)
    {
        services.RemoveAll<IPortfolioPerformanceSource>();

        services.AddSingleton<IPortfolioPerformanceSource, PostgresPortfolioPerformanceSource>();
    }

    private static void RegisterPaperTrading(IServiceCollection services)
    {
        services.RemoveAll<IPaperTradingStore>();
        services.RemoveAll<IPaperPortfolioPositionSource>();

        services.AddSingleton<PostgresPaperTradingStore>();
        services.AddSingleton<IPaperTradingStore>(sp => sp.GetRequiredService<PostgresPaperTradingStore>());
        services.AddSingleton<IPaperPortfolioPositionSource>(sp => sp.GetRequiredService<PostgresPaperTradingStore>());
    }

    private static void RegisterEventStore(IServiceCollection services)
    {
        services.AddSingleton<ITradingEventStore, PostgresTradingEventStore>();
        services.AddSingleton<ITradingEventStoreReader, TradingEventStoreReader>();
    }

    private static void RegisterReplayEngine(IServiceCollection services)
    {
        services.AddSingleton<PostgresReplayStore>();
        services.AddSingleton<IReplayJobStore>(sp => sp.GetRequiredService<PostgresReplayStore>());
        services.AddSingleton<IReplayEventSource>(sp => sp.GetRequiredService<PostgresReplayStore>());
        services.AddSingleton<IReplayStrategyEvaluator, RecordedStrategyEvaluator>();
        services.AddSingleton<ReplayEngine.Engine.ReplayEngine>();
    }
}

 