using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Risk;
using TradingSystem.Application.Strategies;
using TradingSystem.Application.Time;
using TradingSystem.BotRuntime.Configuration;
using TradingSystem.BotRuntime.Runtime;
using TradingSystem.Domain.Signals;
using TradingSystem.EventStore;

namespace TradingSystem.Application.Engine;

public sealed class TradingEngine
{
    private readonly ITradingStrategyResolver _strategyResolver;
    private readonly IMarketPriceProvider _marketPriceProvider;
    private readonly IActivePositionProvider _activePositionProvider;
    private readonly ITradeExecutor _tradeExecutor;
    private readonly ITradingOperationLockProvider _lockProvider;
    private readonly ISignalIdempotencyStore _idempotencyStore;
    private readonly ISignalCooldownStore _cooldownStore;
    private readonly ITradingEngineNotifier _notifier;
    private readonly ICentralRiskManager _riskManager;
    private readonly IBotRuntimeStateProvider _runtimeStateProvider;
    private readonly IBotRuntimeConfigurationProvider _runtimeConfigurationProvider;
    private readonly IClock _clock;
    private readonly TradingEngineOptions _options;
    private readonly ILogger<TradingEngine> _logger;
    private readonly ITradingEventStore _eventStore;

    public TradingEngine(
        ITradingStrategyResolver strategyResolver, 
        IMarketPriceProvider marketPriceProvider, 
        IActivePositionProvider activePositionProvider, 
        ITradeExecutor tradeExecutor, 
        ITradingOperationLockProvider lockProvider, 
        ISignalIdempotencyStore idempotencyStore, 
        ISignalCooldownStore cooldownStore, 
        ITradingEngineNotifier notifier, 
        ICentralRiskManager riskManager, 
        IBotRuntimeStateProvider runtimeStateProvider, 
        IBotRuntimeConfigurationProvider runtimeConfigurationProvider, 
        IClock clock, 
        IOptions<TradingEngineOptions> options, 
        ILogger<TradingEngine> logger, 
        ITradingEventStore eventStore)
    {
        _strategyResolver  =  strategyResolver;
        _marketPriceProvider  =  marketPriceProvider;
        _activePositionProvider  =  activePositionProvider;
        _tradeExecutor  =  tradeExecutor;
        _lockProvider  =  lockProvider;
        _idempotencyStore  =  idempotencyStore;
        _cooldownStore  =  cooldownStore;
        _notifier  =  notifier;
        _riskManager  =  riskManager;
        _runtimeStateProvider  =  runtimeStateProvider;
        _runtimeConfigurationProvider  =  runtimeConfigurationProvider;
        _clock  =  clock;
        _options  =  options.Value;
        _logger  =  logger;
        _eventStore  =  eventStore;
    }

    public async Task<TradingEngineResult> ProcessSignalAsync(TradeSignal signal,  CancellationToken cancellationToken)
    {
        ValidateSignal(signal);
        var now  =  _clock.UtcNow;
        await AppendEventAsync(signal,  TradingEventTypes.SignalReceived,  new { signal.Side,  signal.Source,  signal.GeneratedAtUtc },  cancellationToken);

        var timestampError  =  ValidateTimestamp(signal,  now);
        if (timestampError is not null)
            return timestampError;

        if (!await _idempotencyStore.TryStartAsync(signal.SignalId,  _options.ProcessingIdempotencyTtl,  cancellationToken))
            return new(true,  false,  true,  "Duplicate signal ignored.");

        try
        {
            await using var operationLock  =  await _lockProvider.TryAcquireAsync(
                signal.BotName,  signal.Symbol,  signal.Side,  _options.OperationLockTtl,  cancellationToken);

            if (operationLock is null)
            {
                await _idempotencyStore.ReleaseAsync(signal.SignalId,  cancellationToken);
                return new(false,  false,  false,  "Another operation is already processing this bot/symbol/side.");
            }

            var runtimeState  =  await _runtimeStateProvider.GetRequiredAsync(signal.BotName,  cancellationToken);
            if (!runtimeState.AcceptsNewSignals)
            {
                await CompleteSignalAsync(signal.SignalId,  cancellationToken);
                return new(true,  false,  false,  $"BOT_RUNTIME_BLOCKED [{runtimeState.Status}]: {runtimeState.Reason ?? "Bot does not accept new signals."}");
            }

            var runtimeConfiguration  =  await _runtimeConfigurationProvider.GetAsync(signal.BotName,  cancellationToken);
            if (runtimeConfiguration is not null)
            {
                if (!runtimeConfiguration.Symbol.Equals(signal.Symbol,  StringComparison.OrdinalIgnoreCase))
                {
                    await CompleteSignalAsync(signal.SignalId,  cancellationToken);
                    return new(true,  false,  false,  $"Dynamic configuration does not support symbol '{signal.Symbol}'.");
                }
                if (signal.Side == TradingSystem.Domain.Enums.PositionSide.Long  &&  !runtimeConfiguration.EnableLong  || 
                    signal.Side == TradingSystem.Domain.Enums.PositionSide.Short  &&  !runtimeConfiguration.EnableShort)
                {
                    await CompleteSignalAsync(signal.SignalId,  cancellationToken);
                    return new(true,  false,  false,  $"{signal.Side} is disabled by dynamic configuration version {runtimeConfiguration.Version}.");
                }
            }

            var remainingCooldown  =  await _cooldownStore.GetRemainingAsync(
                signal.BotName,  signal.Symbol,  signal.Side,  now,  cancellationToken);

            if (remainingCooldown is not null)
            {
                await CompleteSignalAsync(signal.SignalId,  cancellationToken);
                return new(true,  false,  false,  $"Cooldown active. Remaining = {remainingCooldown.Value:g}.");
            }

            var strategy  =  await _strategyResolver.ResolveAsync(signal.BotName,  cancellationToken);
            ValidateSupportedSymbol(strategy,  signal.Symbol);

            var markPrice  =  await _marketPriceProvider.GetMarkPriceAsync(signal.Symbol,  cancellationToken);
            if (markPrice <= 0)
                throw new InvalidOperationException($"Invalid mark price '{markPrice}' for symbol '{signal.Symbol}'.");

            var activePositions  =  await _activePositionProvider.GetActivePositionsAsync(signal.BotName,  signal.Symbol,  cancellationToken);
            var context  =  new StrategyContext
            {
                Signal  =  signal, 
                MarkPrice  =  markPrice, 
                ActivePositions  =  activePositions, 
                EvaluatedAtUtc  =  now, 
                RuntimeConfiguration  =  runtimeConfiguration
            };

            var decision  =  await strategy.DecideAsync(context,  cancellationToken);
            await AppendEventAsync(signal,  TradingEventTypes.StrategyDecisionTaken,  new { Strategy  =  strategy.Metadata.Name,  strategy.Metadata.Version,  Decision  =  decision.Type.ToString(),  decision.Reason,  decision.PositionsToClose },  cancellationToken);
            await _notifier.DecisionMadeAsync(signal,  markPrice,  decision,  cancellationToken);

            if (!decision.ShouldOpen)
            {
                await CompleteSignalAsync(signal.SignalId,  cancellationToken);
                return new(true,  false,  false,  decision.Reason);
            }

            var riskDecision  =  await _riskManager.EvaluateOpenAsync(new RiskEvaluationContext
            {
                Signal  =  signal, 
                MarkPrice  =  markPrice, 
                ActivePositions  =  activePositions, 
                EvaluatedAtUtc  =  now
            },  cancellationToken);

            await AppendEventAsync(signal,  TradingEventTypes.RiskDecisionTaken,  new { riskDecision.Allowed,  riskDecision.Code,  riskDecision.Reason },  cancellationToken);

            if (!riskDecision.Allowed)
            {
                await CompleteSignalAsync(signal.SignalId,  cancellationToken);
                return new(true,  false,  false,  $"RISK_BLOCKED [{riskDecision.Code}]: {riskDecision.Reason}");
            }

            if (decision.ShouldClosePositions)
            {
                var closeResult  =  await CloseRequiredPositionsAsync(signal.BotName,  decision,  cancellationToken);
                if (!closeResult.Succeeded)
                {
                    await _idempotencyStore.ReleaseAsync(signal.SignalId,  cancellationToken);
                    return new(false,  false,  false,  closeResult.Reason);
                }
            }

            await AppendEventAsync(signal,  TradingEventTypes.ExecutionRequested,  new { signal.BotName,  signal.Symbol,  signal.Side },  cancellationToken);
            var execution  =  await _tradeExecutor.OpenAsync(
                signal.BotName,  signal.Symbol,  signal.Side,  signal.Source,  cancellationToken);

            await _notifier.ExecutionCompletedAsync(signal,  execution,  cancellationToken);
            await AppendEventAsync(signal,  execution.Succeeded ? TradingEventTypes.ExecutionCompleted : TradingEventTypes.ExecutionFailed,  new { execution.Succeeded,  execution.ShortId,  execution.Reason },  cancellationToken,  execution.ShortId);

            if (!execution.Succeeded)
            {
                await _idempotencyStore.ReleaseAsync(signal.SignalId,  cancellationToken);
                return new(false,  false,  false,  execution.Reason);
            }

            var cooldown  =  runtimeConfiguration is not null
                ? TimeSpan.FromSeconds(runtimeConfiguration.CooldownSeconds)
                : strategy is IHasSignalCooldown configurable ? configurable.SignalCooldown : TimeSpan.Zero;
            if (cooldown > TimeSpan.Zero)
                await _cooldownStore.SetAsync(signal.BotName,  signal.Symbol,  signal.Side,  now.Add(cooldown),  cancellationToken);

            await CompleteSignalAsync(signal.SignalId,  cancellationToken);
            return new(true,  true,  false,  execution.Reason,  execution.ShortId);
        }
        catch (Exception ex)
        {
            await _idempotencyStore.ReleaseAsync(signal.SignalId,  cancellationToken);
            await _notifier.ProcessingFailedAsync(signal,  ex,  cancellationToken);
            _logger.LogError(ex,  "Trading signal failed. SignalId = {SignalId},  Bot = {Bot},  Symbol = {Symbol},  Side = {Side}", 
                signal.SignalId,  signal.BotName,  signal.Symbol,  signal.Side);
            return new(false,  false,  false,  ex.Message);
        }
    }

    private async Task<TradeExecutionResult> CloseRequiredPositionsAsync(string botName,  StrategyDecision decision,  CancellationToken cancellationToken)
    {
        foreach (var shortId in decision.PositionsToClose.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var result  =  await _tradeExecutor.CloseAsync(botName,  shortId,  "Strategy requested close before opening replacement position.",  cancellationToken);
            if (!result.Succeeded)
                return TradeExecutionResult.Failure($"Could not close position '{shortId}': {result.Reason}",  result.Exception);
        }
        return TradeExecutionResult.Success("close-batch",  "Required positions closed.");
    }

    private Task CompleteSignalAsync(string signalId,  CancellationToken cancellationToken)
         =>  _idempotencyStore.MarkCompletedAsync(signalId,  _options.CompletedIdempotencyTtl,  cancellationToken);

    private async Task AppendEventAsync(TradeSignal signal,  string eventType,  object payload,  CancellationToken cancellationToken,  string? positionId  =  null)
         =>  _  =  await _eventStore.AppendAsync(new AppendTradingEvent(
            EventType: eventType, 
            AggregateType: "Signal", 
            AggregateId: signal.SignalId, 
            Payload: payload, 
            OccurredAtUtc: _clock.UtcNow, 
            BotName: signal.BotName, 
            Symbol: signal.Symbol, 
            PositionId: positionId, 
            SignalId: signal.SignalId, 
            CorrelationId: signal.SignalId, 
            CausationId: signal.SignalId, 
            Actor: signal.Source),  cancellationToken);

    private TradingEngineResult? ValidateTimestamp(TradeSignal signal,  DateTime now)
    {
        if (signal.GeneratedAtUtc > now.Add(_options.MaximumFutureClockSkew))
            return new(false,  false,  false,  "Signal timestamp is in the future.");
        if (now - signal.GeneratedAtUtc > _options.MaximumSignalAge)
            return new(false,  false,  false,  $"Signal is stale. Age = {now - signal.GeneratedAtUtc:g}.");
        return null;
    }

    private static void ValidateSignal(TradeSignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        if (string.IsNullOrWhiteSpace(signal.SignalId)) throw new ArgumentException("SignalId is required.",  nameof(signal));
        if (string.IsNullOrWhiteSpace(signal.BotName)) throw new ArgumentException("BotName is required.",  nameof(signal));
        if (string.IsNullOrWhiteSpace(signal.Symbol)) throw new ArgumentException("Symbol is required.",  nameof(signal));
        if (string.IsNullOrWhiteSpace(signal.Source)) throw new ArgumentException("Source is required.",  nameof(signal));
    }

    private static void ValidateSupportedSymbol(ITradingStrategy strategy,  string symbol)
    {
        if (!strategy.Metadata.SupportedSymbols.Any(x  =>  x.Equals(symbol,  StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Strategy '{strategy.Metadata.Name}' does not support symbol '{symbol}'.");
    }
}
