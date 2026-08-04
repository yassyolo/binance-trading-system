using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingSystem.Analytics.Abstractions;
using TradingSystem.Application.Risk;
using TradingSystem.BotRuntime.Commands;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Runtime;
using TradingSystem.EventStore;
using TradingSystem.Observability.History;
using TradingSystem.Operations;
using TradingSystem.PaperTrading;
using TradingSystem.Persistence.PostgreSql.Analytics;
using TradingSystem.Persistence.PostgreSql.BotRuntime;
using TradingSystem.Persistence.PostgreSql.Connections;
using TradingSystem.Persistence.PostgreSql.EventStore;
using TradingSystem.Persistence.PostgreSql.Operations;
using TradingSystem.Persistence.PostgreSql.PaperTrading;
using TradingSystem.Persistence.PostgreSql.Portfolio;
using TradingSystem.Persistence.PostgreSql.Reliability;
using TradingSystem.Persistence.PostgreSql.Replay;
using TradingSystem.Persistence.PostgreSql.TradingHistory;
using TradingSystem.PortfolioManagement;
using TradingSystem.Reconciliation;
using TradingSystem.ReplayEngine;

namespace TradingSystem.Persistence.PostgreSql.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresTradingHistory(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PostgresTradingHistoryOptions.SectionName)
            .Get<PostgresTradingHistoryOptions>() ?? new();

        var connectionString = configuration.GetConnectionString(
            options.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Missing connection string '{options.ConnectionStringName}'.");

        services.AddSingleton<ITradingDbConnectionFactory>(
            new NpgsqlTradingDbConnectionFactory(
                connectionString,
                Math.Max(1, options.CommandTimeoutSeconds)));

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

        services.AddSingleton<
            IRiskStateProvider,
            PostgresRiskStateProvider>();

        services.AddSingleton<
            IReconciliationFindingStore,
            PostgresReconciliationFindingStore>();
    }

    private static void RegisterBotRuntime(
        IServiceCollection services)
    {
        services.AddSingleton<
            IBotRuntimeStateStore,
            PostgresBotRuntimeStateStore>();

        services.AddSingleton<
            IBotRuntimeConfigurationStore,
            PostgresBotRuntimeConfigurationStore>();

        services.AddSingleton<
            IBotCommandQueue,
            PostgresBotCommandQueue>();
    }

    private static void RegisterOperations(
        IServiceCollection services)
    {
        services.AddSingleton<PostgresOperationalStore>();

        services.AddSingleton<IServiceHeartbeatStore>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PostgresOperationalStore>());

        services.AddSingleton<IAlertStore>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PostgresOperationalStore>());

        services.AddSingleton<IAuditLog>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PostgresOperationalStore>());

        services.AddSingleton<
            IAlertCandidateSource,
            OperationalAlertCandidateSource>();
    }

    private static void RegisterPortfolio(
        IServiceCollection services)
    {
        services.RemoveAll<IPortfolioPerformanceSource>();

        services.AddSingleton<
            IPortfolioPerformanceSource,
            PostgresPortfolioPerformanceSource>();
    }

    private static void RegisterPaperTrading(
        IServiceCollection services)
    {
        services.RemoveAll<IPaperTradingStore>();
        services.RemoveAll<IPaperPortfolioPositionSource>();

        services.AddSingleton<PostgresPaperTradingStore>();

        services.AddSingleton<IPaperTradingStore>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PostgresPaperTradingStore>());

        services.AddSingleton<IPaperPortfolioPositionSource>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PostgresPaperTradingStore>());
    }

    private static void RegisterEventStore(
        IServiceCollection services)
    {
        services.AddSingleton<
            ITradingEventStore,
            PostgresTradingEventStore>();

        services.AddSingleton<
            ITradingTimelineReader,
            TradingTimelineReader>();
    }

    private static void RegisterReplayEngine(
        IServiceCollection services)
    {
        services.AddSingleton<PostgresReplayStore>();

        services.AddSingleton<IReplayJobStore>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PostgresReplayStore>());

        services.AddSingleton<IReplayEventSource>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    PostgresReplayStore>());

        services.AddSingleton<
            IReplayStrategyEvaluator,
            RecordedStrategyEvaluator>();

        services.AddSingleton<
            TradingSystem.ReplayEngine.ReplayEngine>();
    }
}

 