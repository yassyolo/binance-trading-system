using TradingSystem.Application.Engine;
using TradingSystem.Application.Engine.Models;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Signals;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History.Models;
using TradingSystem.Observability.Pipeline;

namespace StrategyService.Services;

public sealed class TradingSignalHandler(
    TradingEngine engine,
    TradingStrategyRegistry strategies,
    ITradingPipelineRecorder history,
    ITradingEnvironmentProvider environment,
    ITradingSignalContextAccessor signalContext,
    ILogger<TradingSignalHandler> logger)
    : ITradingSignalHandler
{
    public async Task<bool> HandleAsync(TradeSignal signal, CancellationToken ct)
    {
        var strategyVersion = ResolveStrategyVersion(signal.BotName);

        await TryRecordSignalAsync(signal, strategyVersion, ct);

        using var contextScope = signalContext.Push(new TradingSignalExecutionContext(signal.SignalId, strategyVersion, signal.Source));

        TradingEngineResult result;

        try
        {
            result = await engine.ProcessSignalAsync(signal, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await TryRecordDecisionAsync(
                signal,
                strategyVersion,
                decision: "Failed",
                reason: exception.Message,
                metadata: new Dictionary<string, object?>
                {
                    ["exceptionType"] = exception.GetType().FullName
                },
                ct);

            logger.LogError(
                exception,
                "Signal handler failed. Id = {Id} Bot = {Bot}",
                signal.SignalId,
                signal.BotName);

            return false;
        }

        await TryRecordDecisionAsync(
            signal,
            strategyVersion,
            ResolveFinalDecision(result),
            result.Reason,
            new Dictionary<string, object?>
            {
                ["succeeded"] = result.Succeeded,
                ["openedPosition"] = result.OpenedPosition,
                ["duplicate"] = result.Duplicate,
                ["shortId"] = result.ShortId
            },
            ct);

        logger.LogInformation(
            "Signal processed. Id = {Id} Bot = {Bot} Opened = {Opened} Duplicate = {Duplicate} Reason = {Reason}",
            signal.SignalId,
            signal.BotName,
            result.OpenedPosition,
            result.Duplicate,
            result.Reason);

        return result.Succeeded;
    }

    private async Task TryRecordSignalAsync(
        TradeSignal signal,
        string strategyVersion,
        CancellationToken ct)
    {
        try
        {
            var metadata = signal.Metadata.ToDictionary(
                pair => pair.Key,
                pair => (object?)pair.Value,
                StringComparer.OrdinalIgnoreCase);

            metadata["receivedAtUtc"] = DateTime.UtcNow;

            await history.RecordSignalAsync(
                new SignalHistoryRecord(
                    SignalId: signal.SignalId,
                    BotName: signal.BotName,
                    StrategyVersion: strategyVersion,
                    Symbol: signal.Symbol,
                    Side: signal.Side.ToString(),
                    Source: signal.Source,
                    Environment: environment.EnvironmentName,
                    SignalTimeUtc: signal.GeneratedAtUtc,
                    ReferencePrice: signal.SuggestedPrice,
                    CandleOpenTimeUtc: null,
                    Interval: null,
                    Reason: null,
                    RawPayload: signal.RawPayload,
                    Metadata: metadata),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Observability must not prevent a valid trading signal from being processed.
            logger.LogError(
                exception,
                "Signal history could not be recorded. SignalId = {SignalId}",
                signal.SignalId);
        }
    }

    private async Task TryRecordDecisionAsync(
        TradeSignal signal,
        string strategyVersion,
        string decision,
        string? reason,
        IReadOnlyDictionary<string, object?>? metadata,
        CancellationToken ct)
    {
        try
        {
            await history.RecordDecisionAsync(
                new DecisionHistoryRecord(
                    SignalId: signal.SignalId,
                    BotName: signal.BotName,
                    StrategyVersion: strategyVersion,
                    Symbol: signal.Symbol,
                    Side: signal.Side.ToString(),
                    Decision: decision,
                    Reason: reason,
                    Environment: environment.EnvironmentName,
                    DecidedAtUtc: DateTime.UtcNow,
                    MarkPrice: null,
                    Parameters: null,
                    Metadata: metadata),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Final signal decision could not be recorded. SignalId = {SignalId}",
                signal.SignalId);
        }
    }

    private string ResolveStrategyVersion(string botName)
    {
        try
        {
            return strategies.GetRequired(botName).Metadata.Version;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Strategy version could not be resolved for bot {Bot}. History will use 'unknown'.",
                botName);

            return "unknown";
        }
    }

    private static string ResolveFinalDecision(TradingEngineResult result)
    {
        if (result.Duplicate)
            return "Duplicate";

        if (result.OpenedPosition)
            return "Open";

        if (IsTimestampRejection(result.Reason))
            return "Rejected";

        if (result.Succeeded)
            return "Block";

        return "Failed";
    }

    private static bool IsTimestampRejection(string? reason)
    {
        return reason?.Contains("Signal is stale", StringComparison.OrdinalIgnoreCase) == true ||
               reason?.Contains("Signal timestamp is in the future", StringComparison.OrdinalIgnoreCase) == true;
    }
}
