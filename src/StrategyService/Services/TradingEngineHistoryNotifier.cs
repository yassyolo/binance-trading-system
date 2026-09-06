using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Strategies;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Domain.Signals;
using TradingSystem.HistoricalDatabase.EventStore;
using TradingSystem.HistoricalDatabase.Models;
using TradingSystem.HistoricalDatabase.Models.Enums;
using TradingSystem.Observability.Environment;
using TradingSystem.Prometheus.PrometheusMetrics;

namespace StrategyService.Services;

public sealed class TradingEngineHistoryNotifier(
    TelegramTradingEngineNotifier telegramNotifier,
    TradingStrategyRegistry strategyRegistry,
    IPositionStore positionStore,
    IHistoricalEventSink historicalDatabase,
    TradingMetrics metrics,
    ITradingEnvironmentProvider envProvider,
    IBotRuntimeConfigurationProvider configProvider,
    LivePositionLifecycleRecorder lifecycleRecorder)
    : ITradingEngineNotifier
{
    public async Task DecisionMadeAsync(TradeSignal signal, decimal markPrice, StrategyDecision decision, CancellationToken ct)
    {
        var strategy = strategyRegistry.GetRequired(signal.BotName);
        var decisionName = decision.ShouldOpen ? "Open" : "Block";
        var environmentName = await ResolveEnvironmentAsync(signal.BotName, ct);

        metrics.SignalsReceived.WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), signal.Source ?? "unknown").Inc();
        metrics.Decisions.WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), decisionName).Inc();

        await historicalDatabase.WriteAsync(
            new HistoricalEvent(
                Guid.NewGuid(),
                HistoricalEventType.StrategyDecision,
                DateTime.UtcNow,
                environmentName,
                signal.SignalId,
                signal.BotName,
                strategy.Metadata.Version,
                signal.Symbol,
                null,
                null,
                signal.Side.ToString(),
                decisionName,
                markPrice,
                null,
                null,
                decision.Reason,
                new Dictionary<string, object?>
                {
                    ["positions_to_close"] = decision.PositionsToClose,
                    ["source"] = signal.Source
                }),
            ct);

        await telegramNotifier.DecisionMadeAsync(signal, markPrice, decision, ct);
    }

    public async Task ExecutionCompletedAsync(TradeSignal signal, TradeExecutionResult result, CancellationToken ct)
    {
        metrics.Executions.WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), result.Succeeded ? "success" : "failure").Inc();

        var environmentName = await ResolveEnvironmentAsync(signal.BotName, ct);
        string? strategyVersion = null;

        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.ShortId))
        {
            var position = await positionStore.GetAsync(signal.BotName, result.ShortId, ct);

            if (position is not null)
            {
                var strategy = strategyRegistry.GetRequired(signal.BotName);
                strategyVersion = strategy.Metadata.Version;
                
                await lifecycleRecorder.RecordOpenedAsync(position, signal.SignalId, strategyVersion, ct);
            }
        }

        await historicalDatabase.WriteAsync(
            new HistoricalEvent(
                Guid.NewGuid(),
                HistoricalEventType.ExecutionCompleted,
                DateTime.UtcNow,
                environmentName,
                signal.SignalId,
                signal.BotName,
                strategyVersion,
                signal.Symbol,
                result.ShortId,
                null,
                signal.Side.ToString(),
                result.Succeeded ? "SUCCEEDED" : "FAILED",
                null,
                null,
                null,
                result.Reason,
                new Dictionary<string, object?>
                {
                    ["exception_type"] = result.Exception?.GetType().Name
                }),
            ct);

        await telegramNotifier.ExecutionCompletedAsync(signal, result, ct);
    }

    public async Task ProcessingFailedAsync(TradeSignal signal, Exception exception, CancellationToken ct)
    {
        metrics.ProcessingFailures.WithLabels("trading_engine", signal.BotName, exception.GetType().Name).Inc();
       
        var environmentName = await ResolveEnvironmentAsync(signal.BotName, ct);

        await historicalDatabase.WriteAsync(
            new HistoricalEvent(
                Guid.NewGuid(),
                HistoricalEventType.ProcessingFailed,
                DateTime.UtcNow,
                environmentName,
                signal.SignalId,
                signal.BotName,
                null,
                signal.Symbol,
                null,
                null,
                signal.Side.ToString(),
                "FAILED",
                null,
                null,
                null,
                exception.Message,
                new Dictionary<string, object?>
                {
                    ["exception_type"] = exception.GetType().FullName
                }),
            ct);

        await telegramNotifier.ProcessingFailedAsync(signal, exception, ct);
    }

    private async Task<string> ResolveEnvironmentAsync(string botName, CancellationToken ct)
    {
        var config = await configProvider.GetAsync(botName, ct);

        return config is not null && !string.IsNullOrWhiteSpace(config.Environment)
            ? config.Environment.Trim()
            : envProvider.EnvironmentName;
    }
}
