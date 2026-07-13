using Microsoft.Extensions.Logging;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Engine;

public sealed class TradingEngine
{
    private static readonly TimeSpan ProcessingIdempotencyTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CompletedIdempotencyTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan OperationLockTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxSignalAge = TimeSpan.FromMinutes(2);

    private readonly CompositeTradingStrategy _strategies;
    private readonly IMarketPriceProvider _marketPriceProvider;
    private readonly IActivePositionProvider _activePositionProvider;
    private readonly ITradeExecutor _tradeExecutor;
    private readonly ITradingOperationLockProvider _lockProvider;
    private readonly ISignalIdempotencyStore _idempotencyStore;
    private readonly ISignalCooldownStore _cooldownStore;
    private readonly ITradingEngineNotifier _notifier;
    private readonly IClock _clock;
    private readonly ILogger<TradingEngine> _logger;

    public TradingEngine(
        CompositeTradingStrategy strategies,
        IMarketPriceProvider marketPriceProvider,
        IActivePositionProvider activePositionProvider,
        ITradeExecutor tradeExecutor,
        ITradingOperationLockProvider lockProvider,
        ISignalIdempotencyStore idempotencyStore,
        ISignalCooldownStore cooldownStore,
        ITradingEngineNotifier notifier,
        IClock clock,
        ILogger<TradingEngine> logger)
    {
        _strategies = strategies;
        _marketPriceProvider = marketPriceProvider;
        _activePositionProvider = activePositionProvider;
        _tradeExecutor = tradeExecutor;
        _lockProvider = lockProvider;
        _idempotencyStore = idempotencyStore;
        _cooldownStore = cooldownStore;
        _notifier = notifier;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TradingEngineResult> ProcessSignalAsync(
        TradeSignal signal,
        CancellationToken cancellationToken)
    {
        ValidateSignal(signal);

        var now = _clock.UtcNow;

        if (signal.GeneratedAtUtc > now.AddSeconds(10))
        {
            return new TradingEngineResult(
                false,
                false,
                false,
                "Signal timestamp is in the future.");
        }

        if (now - signal.GeneratedAtUtc > MaxSignalAge)
        {
            return new TradingEngineResult(
                false,
                false,
                false,
                $"Signal is stale. Age={now - signal.GeneratedAtUtc:g}.");
        }

        var started = await _idempotencyStore.TryStartAsync(
            signal.SignalId,
            ProcessingIdempotencyTtl,
            cancellationToken);

        if (!started)
        {
            return new TradingEngineResult(
                true,
                false,
                true,
                "Duplicate signal ignored.");
        }

        try
        {
            await using var operationLock = await _lockProvider.TryAcquireAsync(
                signal.BotName,
                signal.Symbol,
                signal.Side,
                OperationLockTtl,
                cancellationToken);

            if (operationLock is null)
            {
                await _idempotencyStore.ReleaseAsync(
                    signal.SignalId,
                    cancellationToken);

                return new TradingEngineResult(
                    false,
                    false,
                    false,
                    "Another operation is already processing this bot/symbol/side.");
            }

            var remainingCooldown = await _cooldownStore.GetRemainingAsync(
                signal.BotName,
                signal.Symbol,
                signal.Side,
                now,
                cancellationToken);

            if (remainingCooldown is not null)
            {
                await _idempotencyStore.MarkCompletedAsync(
                    signal.SignalId,
                    CompletedIdempotencyTtl,
                    cancellationToken);

                return new TradingEngineResult(
                    true,
                    false,
                    false,
                    $"Cooldown active. Remaining={remainingCooldown.Value:g}.");
            }

            var strategy = _strategies.GetRequired(signal.BotName);
            ValidateSupportedSymbol(strategy, signal.Symbol);

            var markPrice = await _marketPriceProvider.GetMarkPriceAsync(
                signal.Symbol,
                cancellationToken);

            if (markPrice <= 0)
                throw new InvalidOperationException(
                    $"Invalid mark price '{markPrice}' for symbol '{signal.Symbol}'.");

            var activePositions = await _activePositionProvider.GetActivePositionsAsync(
                signal.BotName,
                signal.Symbol,
                cancellationToken);

            var context = new StrategyContext
            {
                Signal = signal,
                MarkPrice = markPrice,
                ActivePositions = activePositions,
                EvaluatedAtUtc = now
            };

            var decision = await strategy.DecideAsync(
                context,
                cancellationToken);

            await _notifier.DecisionMadeAsync(
                signal,
                markPrice,
                decision,
                cancellationToken);

            if (!decision.ShouldOpen)
            {
                await _idempotencyStore.MarkCompletedAsync(
                    signal.SignalId,
                    CompletedIdempotencyTtl,
                    cancellationToken);

                return new TradingEngineResult(
                    true,
                    false,
                    false,
                    decision.Reason);
            }

            var execution = await _tradeExecutor.OpenAsync(
                signal.BotName,
                signal.Symbol,
                signal.Side,
                signal.Source,
                cancellationToken);

            await _notifier.ExecutionCompletedAsync(
                signal,
                execution,
                cancellationToken);

            if (!execution.Succeeded)
            {
                await _idempotencyStore.ReleaseAsync(
                    signal.SignalId,
                    cancellationToken);

                return new TradingEngineResult(
                    false,
                    false,
                    false,
                    execution.Reason);
            }

            var cooldown = ResolveCooldown(strategy);

            if (cooldown > TimeSpan.Zero)
            {
                await _cooldownStore.SetAsync(
                    signal.BotName,
                    signal.Symbol,
                    signal.Side,
                    now.Add(cooldown),
                    cancellationToken);
            }

            await _idempotencyStore.MarkCompletedAsync(
                signal.SignalId,
                CompletedIdempotencyTtl,
                cancellationToken);

            return new TradingEngineResult(
                true,
                true,
                false,
                execution.Reason,
                execution.ShortId);
        }
        catch (Exception ex)
        {
            await _idempotencyStore.ReleaseAsync(
                signal.SignalId,
                cancellationToken);

            await _notifier.ProcessingFailedAsync(
                signal,
                ex,
                cancellationToken);

            _logger.LogError(
                ex,
                "Trading signal failed. SignalId={SignalId}, Bot={Bot}, Symbol={Symbol}, Side={Side}",
                signal.SignalId,
                signal.BotName,
                signal.Symbol,
                signal.Side);

            return new TradingEngineResult(
                false,
                false,
                false,
                ex.Message);
        }
    }

    private static void ValidateSignal(TradeSignal signal)
    {
        if (string.IsNullOrWhiteSpace(signal.SignalId))
            throw new ArgumentException("SignalId is required.", nameof(signal));

        if (string.IsNullOrWhiteSpace(signal.BotName))
            throw new ArgumentException("BotName is required.", nameof(signal));

        if (string.IsNullOrWhiteSpace(signal.Symbol))
            throw new ArgumentException("Symbol is required.", nameof(signal));
    }

    private static void ValidateSupportedSymbol(
        ITradingStrategy strategy,
        string symbol)
    {
        var supported = strategy.Metadata.SupportedSymbols.Any(
            x => x.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        if (!supported)
        {
            throw new InvalidOperationException(
                $"Strategy '{strategy.Metadata.Name}' does not support symbol '{symbol}'.");
        }
    }

    private static TimeSpan ResolveCooldown(ITradingStrategy strategy)
    {
        return strategy is IHasSignalCooldown configurable
            ? configurable.SignalCooldown
            : TimeSpan.Zero;
    }
}
