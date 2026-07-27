using TradingSystem.Application.Engine;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Signals;
using TradingSystem.HistoricalDatabase;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History;
using TradingSystem.Prometheus;

namespace StrategyService.Services;

public sealed class TradingEngineHistoryNotifier(
    TelegramTradingEngineNotifier notifications,
    TradingStrategyRegistry strategies,
    IPositionStore positions,
    ITradingPipelineRecorder history,
    IHistoricalEventSink historicalEvents,
    TradingMetrics metrics,
    ITradingEnvironmentProvider environment) : ITradingEngineNotifier
{
    public async Task DecisionMadeAsync(
        TradeSignal signal,
        decimal markPrice,
        StrategyDecision decision,
        CancellationToken cancellationToken)
    {
        var strategy = strategies.GetRequired(signal.BotName);
        var decisionName = decision.ShouldOpen ? "Open" : "Block";

        metrics.SignalsReceived
            .WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), signal.Source ?? "unknown")
            .Inc();
        metrics.Decisions
            .WithLabels(signal.BotName, signal.Symbol, signal.Side.ToString(), decisionName)
            .Inc();

        await history.RecordDecisionAsync(new DecisionHistoryRecord(
            signal.SignalId,
            signal.BotName,
            strategy.Metadata.Version,
            signal.Symbol,
            signal.Side.ToString(),
            decisionName,
            decision.Reason,
            environment.EnvironmentName,
            DateTime.UtcNow,
            markPrice,
            new Dictionary<string, object?>
            {
                ["positions_to_close"] = decision.PositionsToClose
            }), cancellationToken);

        await historicalEvents.WriteAsync(new HistoricalEvent(
            Guid.NewGuid(),
            HistoricalEventType.StrategyDecision,
            DateTime.UtcNow,
            environment.EnvironmentName,
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
            }), cancellationToken);

        await notifications.DecisionMadeAsync(signal, markPrice, decision, cancellationToken);
    }

    public async Task ExecutionCompletedAsync(
        TradeSignal signal,
        TradeExecutionResult result,
        CancellationToken cancellationToken)
    {
        metrics.Executions
            .WithLabels(
                signal.BotName,
                signal.Symbol,
                signal.Side.ToString(),
                result.Succeeded ? "success" : "failure")
            .Inc();

        string? strategyVersion = null;
        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.ShortId))
        {
            var position = await positions.GetAsync(
                signal.BotName,
                result.ShortId,
                cancellationToken);

            if (position is not null)
            {
                var strategy = strategies.GetRequired(signal.BotName);
                strategyVersion = strategy.Metadata.Version;

                await history.UpsertPositionAsync(new PositionHistoryRecord(
                    position.ShortId,
                    signal.SignalId,
                    position.BotName,
                    strategy.Metadata.Version,
                    position.Symbol,
                    position.Side.ToString(),
                    position.Source,
                    environment.EnvironmentName,
                    position.Status.ToString(),
                    position.Quantity,
                    position.EntryPrice,
                    position.TpPrice,
                    position.ParentFilledAtUtc ?? position.CreatedAtUtc,
                    position.ClosedAtUtc,
                    null,
                    null,
                    position.CloseStatus), cancellationToken);
            }
        }

        await historicalEvents.WriteAsync(new HistoricalEvent(
            Guid.NewGuid(),
            HistoricalEventType.ExecutionCompleted,
            DateTime.UtcNow,
            environment.EnvironmentName,
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
            }), cancellationToken);

        await notifications.ExecutionCompletedAsync(signal, result, cancellationToken);
    }

    public async Task ProcessingFailedAsync(
        TradeSignal signal,
        Exception exception,
        CancellationToken cancellationToken)
    {
        metrics.ProcessingFailures
            .WithLabels("trading_engine", signal.BotName, exception.GetType().Name)
            .Inc();

        await historicalEvents.WriteAsync(new HistoricalEvent(
            Guid.NewGuid(),
            HistoricalEventType.ProcessingFailed,
            DateTime.UtcNow,
            environment.EnvironmentName,
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
            }), cancellationToken);

        await notifications.ProcessingFailedAsync(signal, exception, cancellationToken);
    }
}
