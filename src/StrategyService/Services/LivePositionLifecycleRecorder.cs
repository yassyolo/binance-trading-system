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
    IPositionStore positionStore,
    IBotRuntimeConfigurationProvider configProvider,
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
                ["env"] = environment,
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
        var position = await positionStore.GetAsync(botName, shortId, ct);
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
                ["env"] = environment,
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
        BotPosition p,
        string env,
        string? signalId,
        string strategyVersion,
        string? closeReasonOverride,
        CancellationToken ct)
    {
        try
        {
            await history.UpsertPositionAsync(new PositionHistoryRecord(
                PositionId: p.ShortId,
                SignalId: signalId,
                BotName: p.BotName,
                StrategyVersion: strategyVersion,
                Symbol: p.Symbol,
                Side: p.Side.ToString(),
                Source: p.Source,
                Environment: env,
                Status: p.Status.ToString(),
                Quantity: p.Quantity,
                EntryPrice: p.EntryPrice,
                TakeProfitPrice: p.TpPrice,
                OpenedAtUtc: p.ParentFilledAtUtc ?? p.CreatedAtUtc,
                ClosedAtUtc: p.ClosedAtUtc,
                RealizedPnl: null,
                Fees: null,
                CloseReason: closeReasonOverride ?? p.CloseStatus,
                Metadata: new Dictionary<string, object?>
                {
                    ["remainingQuantity"] = p.RemainingQuantity,
                    ["protectiveActive"] = p.ProtectiveActive,
                    ["tpStatus"] = p.TpStatus,
                    ["slStatus"] = p.SlStatus,
                    ["stop3Status"] = p.Stop3Status,
                    ["closeOrderId"] = p.CloseOrderId,
                    ["mode"] = p.Mode.ToString()
                }), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,"Live p projection persistence failed. Bot = {Bot}, Position = {Position}", p.BotName, p.ShortId);
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Live p history event persistence failed. Bot = {Bot}, Position = {Position}", record.BotName, record.PositionId);
        }
    }

    private async Task AppendLifecycleBestEffortAsync(
        BotPosition p,
        string eventType,
        string env,
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
                AggregateId: p.ShortId,
                Payload: payload,
                OccurredAtUtc: occurredAtUtc,
                BotName: p.BotName,
                Symbol: p.Symbol,
                PositionId: p.ShortId,
                SignalId: signalId,
                CorrelationId: signalId ?? p.ShortId,
                Actor: p.Source ?? "strategy",
                Metadata: new Dictionary<string, object?> { ["env"] = env }), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Live position EventStore append failed. Type = {Type}, Bot = {Bot}, Position = {Position}", eventType, p.BotName, p.ShortId);
        }
    }

    private async Task<string> ResolveEnvironmentAsync(string botName, CancellationToken ct)
    {
        try
        {
            var config = await configProvider.GetAsync(botName, ct);
            if (config is not null && !string.IsNullOrWhiteSpace(config.Environment))
                return config.Environment.Trim();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Runtime env could not be resolved for {Bot}. Falling back to observability env.", botName);
        }

        return fallbackEnvironment.EnvironmentName;
    }

    private static bool IsLive(string environment) =>
        environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
        environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
