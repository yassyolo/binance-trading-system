using TradingSystem.Application.Positions.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Domain.Positions;
using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History.Models;
using TradingSystem.Observability.Pipeline;

namespace StrategyService.Services;

public sealed class LivePositionLifecycleRecorder(
    IPositionStore positions,
    IBotRuntimeConfigurationProvider configurations,
    ITradingPipelineRecorder history,
    ITradingEventStore eventStore,
    ITradingEnvironmentProvider fallbackEnvironment,
    ILogger<LivePositionLifecycleRecorder> logger)
{
    private const string UnknownStrategyVersion = "unknown";

    public async Task RecordOpenedAsync(BotPosition position, string? signalId, string strategyVersion, CancellationToken ct)
    {
        var environment = await ResolveEnvironmentAsync(position.BotName, ct);
        if (!IsLive(environment))
            return;

        await PersistProjectionBestEffortAsync(position, environment, signalId, strategyVersion, null, ct);

        await TryHistoryEventAsync(new PositionEventHistoryRecord(
            position.ShortId,
            position.BotName,
            "LIVE_POSITION_OPENED",
            position.Status.ToString(),
            position.ParentFilledAtUtc ?? position.CreatedAtUtc,
            position.EntryPrice,
            position.Quantity,
            new Dictionary<string, object?>
            {
                ["environment"] = environment,
                ["signalId"] = signalId,
                ["source"] = position.Source,
                ["takeProfitPrice"] = position.TpPrice
            }), ct);

        await AppendLifecycleBestEffortAsync(
            position,
            TradingEventTypes.PositionOpened,
            environment,
            signalId,
            position.ParentFilledAtUtc ?? position.CreatedAtUtc,
            new { position.Status, position.Quantity, position.EntryPrice, position.TpPrice, position.Source },
            ct);
    }

    public async Task RecordClosedAsync(string botName, string shortId, string? reason, CancellationToken ct)
    {
        var position = await positions.GetAsync(botName, shortId, ct);
        if (position is null || !position.Closed)
            return;

        var environment = await ResolveEnvironmentAsync(botName, ct);
        if (!IsLive(environment))
            return;

        await PersistProjectionBestEffortAsync(position, environment, null, UnknownStrategyVersion, reason, ct);

        await TryHistoryEventAsync(new PositionEventHistoryRecord(
            position.ShortId,
            position.BotName,
            "LIVE_POSITION_CLOSED",
            position.Status.ToString(),
            (position.ClosedAtUtc ?? position.UpdatedAtUtc) ?? DateTime.UtcNow,
            null,
            position.Quantity,
            new Dictionary<string, object?>
            {
                ["environment"] = environment,
                ["reason"] = reason ?? position.CloseStatus,
                ["remainingQuantity"] = position.RemainingQuantity
            }), ct);

        await AppendLifecycleBestEffortAsync(
            position,
            TradingEventTypes.PositionClosed,
            environment,
            null,
            (position.ClosedAtUtc ?? position.UpdatedAtUtc) ?? DateTime.UtcNow,
            new
            {
                position.Status,
                position.CloseStatus,
                position.RemainingQuantity,
                position.CloseOrderId,
                Reason = reason ?? position.CloseStatus
            },
            ct);
    }

    public async Task RepairAsync(BotPosition position, CancellationToken ct)
    {
        var environment = await ResolveEnvironmentAsync(position.BotName, ct);
        if (!IsLive(environment))
            return;

        await PersistProjectionBestEffortAsync(
            position,
            environment,
            null,
            UnknownStrategyVersion,
            position.CloseStatus,
            ct);

        await AppendLifecycleBestEffortAsync(
            position,
            TradingEventTypes.PositionOpened,
            environment,
            null,
            position.ParentFilledAtUtc ?? position.CreatedAtUtc,
            new
            {
                position.Status,
                position.Quantity,
                position.EntryPrice,
                position.TpPrice,
                position.Source,
                Repaired = true
            },
            ct);

        if (position.Closed)
        {
            await AppendLifecycleBestEffortAsync(
                position,
                TradingEventTypes.PositionClosed,
                environment,
                null,
                (position.ClosedAtUtc ?? position.UpdatedAtUtc) ?? DateTime.UtcNow,
                new
                {
                    position.Status,
                    position.CloseStatus,
                    position.RemainingQuantity,
                    position.CloseOrderId,
                    Repaired = true
                },
                ct);
        }
    }

    private async Task PersistProjectionBestEffortAsync(
        BotPosition position,
        string environment,
        string? signalId,
        string strategyVersion,
        string? closeReasonOverride,
        CancellationToken ct)
    {
        try
        {
            await history.UpsertPositionAsync(new PositionHistoryRecord(
                PositionId: position.ShortId,
                SignalId: signalId,
                BotName: position.BotName,
                StrategyVersion: strategyVersion,
                Symbol: position.Symbol,
                Side: position.Side.ToString(),
                Source: position.Source,
                Environment: environment,
                Status: position.Status.ToString(),
                Quantity: position.Quantity,
                EntryPrice: position.EntryPrice,
                TakeProfitPrice: position.TpPrice,
                OpenedAtUtc: position.ParentFilledAtUtc ?? position.CreatedAtUtc,
                ClosedAtUtc: position.ClosedAtUtc,
                RealizedPnl: null,
                Fees: null,
                CloseReason: closeReasonOverride ?? position.CloseStatus,
                Metadata: new Dictionary<string, object?>
                {
                    ["remainingQuantity"] = position.RemainingQuantity,
                    ["protectiveActive"] = position.ProtectiveActive,
                    ["tpStatus"] = position.TpStatus,
                    ["slStatus"] = position.SlStatus,
                    ["stop3Status"] = position.Stop3Status,
                    ["closeOrderId"] = position.CloseOrderId,
                    ["mode"] = position.Mode.ToString()
                }), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Live position projection persistence failed. Bot = {Bot}, Position = {Position}",
                position.BotName, position.ShortId);
        }
    }

    private async Task TryHistoryEventAsync(PositionEventHistoryRecord record, CancellationToken ct)
    {
        try
        {
            await history.RecordPositionEventAsync(record, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Live position history event persistence failed. Bot = {Bot}, Position = {Position}",
                record.BotName, record.PositionId);
        }
    }

    private async Task AppendLifecycleBestEffortAsync(
        BotPosition position,
        string eventType,
        string environment,
        string? signalId,
        DateTime occurredAtUtc,
        object payload,
        CancellationToken ct)
    {
        try
        {
            await eventStore.AppendAsync(new AppendTradingEvent(
                EventType: eventType,
                AggregateType: "Position",
                AggregateId: position.ShortId,
                Payload: payload,
                OccurredAtUtc: occurredAtUtc,
                BotName: position.BotName,
                Symbol: position.Symbol,
                PositionId: position.ShortId,
                SignalId: signalId,
                CorrelationId: signalId ?? position.ShortId,
                Actor: position.Source ?? "strategy",
                Metadata: new Dictionary<string, object?> { ["environment"] = environment }), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Live position EventStore append failed. Type = {Type}, Bot = {Bot}, Position = {Position}",
                eventType, position.BotName, position.ShortId);
        }
    }

    private async Task<string> ResolveEnvironmentAsync(string botName, CancellationToken ct)
    {
        try
        {
            var configuration = await configurations.GetAsync(botName, ct);
            if (configuration is not null && !string.IsNullOrWhiteSpace(configuration.Environment))
                return configuration.Environment.Trim();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Runtime environment could not be resolved for {Bot}. Falling back to observability environment.",
                botName);
        }

        return fallbackEnvironment.EnvironmentName;
    }

    private static bool IsLive(string environment) =>
        environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
        environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
