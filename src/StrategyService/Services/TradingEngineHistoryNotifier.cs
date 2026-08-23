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
    TelegramTradingEngineNotifier notifications,
    TradingStrategyRegistry strategies,
    IPositionStore positions,
    IHistoricalEventSink historicalEvents,
    TradingMetrics metrics,
    ITradingEnvironmentProvider environment,
    IBotRuntimeConfigurationProvider configurations,
    LivePositionLifecycleRecorder lifecycle)
    : ITradingEngineNotifier
{
    public async Task DecisionMadeAsync(TradeSignal signal, decimal markPrice, StrategyDecision decision, CancellationToken ct)
    {
        var strategy = strategies.GetRequired(signal.BotName);
        var decisionName = decision.ShouldOpen ? "Open" : "Block";
        var environmentName = await ResolveEnvironmentAsync(signal.BotName, ct);

        metrics.SignalsReceived.WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), signal.Source ?? "unknown").Inc();
        metrics.Decisions.WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), decisionName).Inc();

        await historicalEvents.WriteAsync(
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

        await notifications.DecisionMadeAsync(signal, markPrice, decision, ct);
    }

    public async Task ExecutionCompletedAsync(TradeSignal signal, TradeExecutionResult result, CancellationToken ct)
    {
        metrics.Executions.WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), result.Succeeded ? "success" : "failure").Inc();

        var environmentName = await ResolveEnvironmentAsync(signal.BotName, ct);
        string? strategyVersion = null;

        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.ShortId))
        {
            var position = await positions.GetAsync(signal.BotName, result.ShortId, ct);

            if (position is not null)
            {
                var strategy = strategies.GetRequired(signal.BotName);
                strategyVersion = strategy.Metadata.Version;
                await lifecycle.RecordOpenedAsync(position, signal.SignalId, strategyVersion, ct);
            }
        }

        await historicalEvents.WriteAsync(
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

        await notifications.ExecutionCompletedAsync(signal, result, ct);
    }

    public async Task ProcessingFailedAsync(TradeSignal signal, Exception exception, CancellationToken ct)
    {
        metrics.ProcessingFailures.WithLabels("trading_engine", signal.BotName, exception.GetType().Name).Inc();
        var environmentName = await ResolveEnvironmentAsync(signal.BotName, ct);

        await historicalEvents.WriteAsync(
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

        await notifications.ProcessingFailedAsync(signal, exception, ct);
    }

    private async Task<string> ResolveEnvironmentAsync(string botName, CancellationToken ct)
    {
        var configuration = await configurations.GetAsync(botName, ct);

        return configuration is not null && !string.IsNullOrWhiteSpace(configuration.Environment)
            ? configuration.Environment.Trim()
            : environment.EnvironmentName;
    }
}
