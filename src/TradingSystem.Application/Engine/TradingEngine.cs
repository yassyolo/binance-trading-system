using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine.Configuration;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Engine.Models;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.Application.Risk.Models;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Application.Time;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.BotRuntime.Runtime.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;
using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;

namespace TradingSystem.Application.Engine;

public sealed class TradingEngine(
	ITradingStrategyResolver strategyResolver,
	IMarketPriceProvider marketPriceProvider,
	IActivePositionProvider activePositionProvider,
	ITradeExecutor tradeExecutor,
	ITradingOperationLockProvider lockProvider,
	ISignalIdempotencyStore idempotencyStore,
	ISignalCooldownStore cooldownStore,
	ITradingEngineNotifier tradingEngineNotifier,
	ICentralRiskManager riskManager,
	IRiskAdmissionLifecycle riskAdmissionLifecycle,
	IBotRuntimeStateProvider runtimeStateProvider,
	IBotRuntimeConfigurationProvider runtimeConfigurationProvider,
	IClock clock,
	IOptions<TradingEngineOptions> options,
	ILogger<TradingEngine> logger,
	ITradingEventStore eventStore)
{
	private readonly TradingEngineOptions _options = options.Value;

	public async Task<TradingEngineResult> ProcessSignalAsync(TradeSignal signal, CancellationToken ct)
	{
		ValidateSignal(signal);
		var now = clock.UtcNow;

		await AppendEventAsync(signal, TradingEventTypes.SignalReceived, new { signal.Side, signal.Source, signal.GeneratedAtUtc }, ct);

		var timestampError = ValidateTimestamp(signal, now);
		if (timestampError is not null)
		{
			await AppendEventAsync(signal, TradingEventTypes.StrategyDecisionTaken, new { Decision = "Rejected", timestampError.Reason }, ct);
			return timestampError;
		}

		if (!await idempotencyStore.TryStartAsync(signal.SignalId, _options.ProcessingIdempotencyTtl, ct))
			return new(true, false, true, "Duplicate signal ignored.");

		var executionSucceeded = false;
		var riskAdmissionCreated = false;
		string? executedShortId = null;

		try
		{
			await using var operationLock = await lockProvider.TryAcquireAsync(signal.BotName, signal.Symbol, signal.Side, _options.OperationLockTtl, ct);
			if (operationLock is null)
			{
				await idempotencyStore.ReleaseAsync(signal.SignalId, ct);

				return new(false, false, false, "Another operation is already processing this bot/symbol/side.");
			}

			var runtimeState = await runtimeStateProvider.GetRequiredAsync(signal.BotName, ct);
			if (!runtimeState.AcceptsNewSignals)
				return await CompleteBlockedAsync(signal, $"BOT_RUNTIME_BLOCKED [{runtimeState.Status}]: {runtimeState.Reason ?? "Bot does not accept new signals."}", ct);

			var runtimeConfiguration = await runtimeConfigurationProvider.GetAsync(signal.BotName, ct);
			if (runtimeConfiguration is not null)
			{
				if (!runtimeConfiguration.Symbol.Equals(signal.Symbol, StringComparison.OrdinalIgnoreCase))
					return await CompleteBlockedAsync(signal, $"Dynamic configuration does not support symbol '{signal.Symbol}'.", ct);

				var sideDisabled = signal.Side == PositionSide.Long && !runtimeConfiguration.EnableLong 
					|| signal.Side == PositionSide.Short && !runtimeConfiguration.EnableShort;

				if (sideDisabled)
					return await CompleteBlockedAsync(signal, $"{signal.Side} is disabled by dynamic configuration version {runtimeConfiguration.Version}.", ct);
			}

			var remainingCooldown = await cooldownStore.GetRemainingAsync(signal.BotName, signal.Symbol, signal.Side, now, ct);
			if (remainingCooldown is not null)
				return await CompleteBlockedAsync(signal, $"Cooldown active. Remaining = {remainingCooldown.Value:g}.", ct);

			var strategy = await strategyResolver.ResolveAsync(signal.BotName, ct);
			ValidateSupportedSymbol(strategy, signal.Symbol);

			var markPrice = await marketPriceProvider.GetMarkPriceAsync(signal.Symbol, ct);
			if (markPrice <= 0)
				throw new InvalidOperationException($"Invalid mark price '{markPrice}' for symbol '{signal.Symbol}'.");

			var activePositions = await activePositionProvider.GetActivePositionsAsync(signal.BotName, signal.Symbol, ct);

			var context = new StrategyContext
			{
				Signal = signal,
				MarkPrice = markPrice,
				ActivePositions = activePositions,
				EvaluatedAtUtc = now,
				RuntimeConfiguration = runtimeConfiguration
			};

			var decision = await strategy.DecideAsync(context, ct);
			
			await AppendEventAsync(
				signal,
				TradingEventTypes.StrategyDecisionTaken,
				new
				{
					Strategy = strategy.Metadata.Name,
					strategy.Metadata.Version,
					Decision = decision.Type.ToString(),
					decision.Reason,
					decision.PositionsToClose
				},
				ct);

			await TryNotifyDecisionAsync(signal, markPrice, decision);

			if (!decision.ShouldOpen)
				return await CompleteBlockedAsync(signal, decision.Reason, ct);

			var riskDecision = await riskManager.EvaluateOpenAsync(
				new RiskEvaluationContext
				{
					Signal = signal,
					MarkPrice = markPrice,
					ActivePositions = activePositions,
					EvaluatedAtUtc = now
				},
				ct);

			await AppendEventAsync(signal, TradingEventTypes.RiskDecisionTaken, new { riskDecision.Allowed, riskDecision.Code, riskDecision.Reason }, ct);

			if (!riskDecision.Allowed)
				return await CompleteBlockedAsync(signal, $"RISK_BLOCKED [{riskDecision.Code}]: {riskDecision.Reason}", ct);

			riskAdmissionCreated = true;

			if (decision.ShouldClosePositions)
			{
				var closeResult = await CloseRequiredPositionsAsync(signal.BotName, decision, ct);

				if (!closeResult.Succeeded)
				{
					await idempotencyStore.ReleaseAsync(signal.SignalId, ct);
					return new(false, false, false, closeResult.Reason);
				}
			}

			await AppendEventAsync(signal, TradingEventTypes.ExecutionRequested, new { signal.BotName, signal.Symbol, signal.Side }, ct);

			var execution = await tradeExecutor.OpenAsync(signal.BotName, signal.Symbol, signal.Side, signal.Source, ct);

			await AppendEventAsync(
				signal,
				execution.Succeeded ? TradingEventTypes.ExecutionCompleted : TradingEventTypes.ExecutionFailed,
				new { execution.Succeeded, execution.ShortId, execution.Reason },
				ct,
				execution.ShortId);

			await TryNotifyExecutionAsync(signal, execution);

			if (!execution.Succeeded)
			{
				await idempotencyStore.ReleaseAsync(signal.SignalId, ct);
				return new(false, false, false, execution.Reason);
			}

			executionSucceeded = true;
			executedShortId = execution.ShortId;

			var cooldown = runtimeConfiguration is not null
				? TimeSpan.FromSeconds(runtimeConfiguration.CooldownSeconds)
				: strategy is IHasSignalCooldown configurable
					? configurable.SignalCooldown : TimeSpan.Zero;

			if (cooldown > TimeSpan.Zero)
			{
				try
				{
					await cooldownStore.SetAsync(signal.BotName, signal.Symbol, signal.Side, now.Add(cooldown), ct);
				}
				catch (Exception exception) when (exception is not OperationCanceledException)
				{
					logger.LogCritical(exception, "Position opened but cooldown persistence failed. SignalId = {SignalId}, ShortId = {ShortId}", signal.SignalId, execution.ShortId);
				}
			}

			await MarkCompletedAfterExecutionAsync(signal.SignalId);

			return new(true, true, false, execution.Reason, execution.ShortId);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			if (!executionSucceeded)
				await TryReleaseAsync(signal.SignalId);

			throw;
		}
		catch (Exception ex)
		{
			if (!executionSucceeded)
				await TryReleaseAsync(signal.SignalId);

			await TryNotifyFailureAsync(signal, ex);
			
			logger.LogError(ex, "Trading signal failed. SignalId = {SignalId}, Bot = {Bot}, Symbol = {Symbol}, Side = {Side}, ExecutionSucceeded = {ExecutionSucceeded}, ShortId = {ShortId}",
				signal.SignalId,
				signal.BotName,
				signal.Symbol,
				signal.Side,
				executionSucceeded,
				executedShortId);

			if (executionSucceeded)
				return new(true, true, false, "Position was opened, but post-execution bookkeeping requires operator review.", executedShortId);

			return new(false, false, false, "Trading signal processing failed. Check the service logs using the signal ID.");
		}
		finally
		{
			if (riskAdmissionCreated)
			{
				try
				{
					await riskAdmissionLifecycle.CompleteAsync(signal.SignalId, executionSucceeded, CancellationToken.None);
				}
				catch (Exception ex)
				{
					logger.LogCritical(ex, "Risk admission reservation could not be completed. SignalId = {SignalId}, ExecutionSucceeded = {ExecutionSucceeded}", signal.SignalId, executionSucceeded);
				}
			}
		}
	}

	private async Task<TradingEngineResult> CompleteBlockedAsync(TradeSignal signal, string reason, CancellationToken ct)
	{
		await idempotencyStore.MarkCompletedAsync(signal.SignalId, _options.CompletedIdempotencyTtl, ct);

		return new(true, false, false, reason);
	}

	private async Task MarkCompletedAfterExecutionAsync(string signalId)
	{
		Exception? lastError = null;
		for (var attempt = 1; attempt <= _options.PostExecutionCompletionRetryCount; attempt++)
		{
			try
			{
				await idempotencyStore.MarkCompletedAsync(signalId, _options.CompletedIdempotencyTtl, CancellationToken.None);

				return;
			}
			catch (Exception exception)
			{
				lastError = exception;
				logger.LogWarning(exception, "Could not persist completed idempotency state. SignalId = {SignalId}, Attempt = {Attempt}/{Attempts}", signalId, attempt, _options.PostExecutionCompletionRetryCount);

				if (attempt < _options.PostExecutionCompletionRetryCount && _options.PostExecutionCompletionRetryDelay > TimeSpan.Zero)
					await Task.Delay(_options.PostExecutionCompletionRetryDelay);
			}
		}

		throw new InvalidOperationException($"Position execution succeeded, but completed idempotency state could not be persisted for signal '{signalId}'.", lastError);
	}

	private async Task TryReleaseAsync(string signalId)
	{
		try
		{
			await idempotencyStore.ReleaseAsync(signalId, CancellationToken.None);
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Could not release processing idempotency state. SignalId = {SignalId}", signalId);
		}
	}

	private async Task TryNotifyDecisionAsync(TradeSignal signal, decimal markPrice, StrategyDecision decision)
	{
		try
		{
			await tradingEngineNotifier.DecisionMadeAsync(signal, markPrice, decision, CancellationToken.None);
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Decision notification failed. SignalId = {SignalId}", signal.SignalId);
		}
	}

	private async Task TryNotifyExecutionAsync(TradeSignal signal, TradeExecutionResult result)
	{
		try
		{
			await tradingEngineNotifier.ExecutionCompletedAsync(signal, result, CancellationToken.None);
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Execution notification failed. SignalId = {SignalId}", signal.SignalId);
		}
	}

	private async Task TryNotifyFailureAsync(TradeSignal signal, Exception error)
	{
		try
		{
			await tradingEngineNotifier.ProcessingFailedAsync(signal, error, CancellationToken.None);
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Failure notification failed. SignalId = {SignalId}", signal.SignalId);
		}
	}

	private async Task<TradeExecutionResult> CloseRequiredPositionsAsync(string botName, StrategyDecision decision, CancellationToken ct)
	{
		foreach (var shortId in decision.PositionsToClose.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			var result = await tradeExecutor.CloseAsync(botName, shortId, "Strategy requested close before opening replacement position.", ct);

			if (!result.Succeeded)
				return TradeExecutionResult.Failure($"Could not close position '{shortId}': {result.Reason}", result.Exception);
		}

		return TradeExecutionResult.Success("close-batch", "Required positions closed.");
	}

	private async Task AppendEventAsync(
		TradeSignal signal,
		string eventType,
		object payload,
		CancellationToken ct,
		string? positionId = null)
	{
		var request = new AppendTradingEvent(
			EventType: eventType,
			AggregateType: "Signal",
			AggregateId: signal.SignalId,
			Payload: payload,
			OccurredAtUtc: clock.UtcNow,
			BotName: signal.BotName,
			Symbol: signal.Symbol,
			PositionId: positionId,
			SignalId: signal.SignalId,
			CorrelationId: signal.SignalId,
			CausationId: signal.SignalId,
			Actor: signal.Source);

		Exception? lastError = null;

		for (var attempt = 1; attempt <= 5; attempt++)
		{
			try
			{
				_ = await eventStore.AppendAsync(request, ct);
				return;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex) when (IsTransientEventStoreFailure(ex))
			{
				lastError = ex;
				logger.LogWarning(ex,"EventStore append failed transiently. SignalId = {SignalId}, EventType = {EventType}, Attempt = {Attempt}/5", signal.SignalId,eventType, attempt);

				if (attempt < 5)
					await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt, 3)), ct);
			}
		}

		throw new InvalidOperationException($"EventStore remained unavailable while persisting '{eventType}' for signal '{signal.SignalId}'.", lastError);
	}

	private static bool IsTransientEventStoreFailure(Exception exception)
	{
		if (exception is TimeoutException)
			return true;

		if (exception.InnerException is TimeoutException)
			return true;

		var typeName = exception.GetType().FullName ?? exception.GetType().Name;
		return typeName.StartsWith("Npgsql.", StringComparison.Ordinal);
	}

	private TradingEngineResult? ValidateTimestamp(TradeSignal signal, DateTime now)
	{
		if (signal.GeneratedAtUtc > now.Add(_options.MaximumFutureClockSkew))
			return new(false, false, false, "Signal timestamp is in the future.");

		if (now - signal.GeneratedAtUtc > _options.MaximumSignalAge)
			return new(false, false, false, $"Signal is stale. Age = {now - signal.GeneratedAtUtc:g}.");
		return null;
	}

	private static void ValidateSignal(TradeSignal signal)
	{
		ArgumentNullException.ThrowIfNull(signal);
		if (string.IsNullOrWhiteSpace(signal.SignalId))
			throw new ArgumentException("SignalId is required.", nameof(signal));
		
		if (string.IsNullOrWhiteSpace(signal.BotName))
			throw new ArgumentException("BotName is required.", nameof(signal));
		
		if (string.IsNullOrWhiteSpace(signal.Symbol))
			throw new ArgumentException("Symbol is required.", nameof(signal));
		
		if (string.IsNullOrWhiteSpace(signal.Source))
			throw new ArgumentException("Source is required.", nameof(signal));
		
		if (signal.GeneratedAtUtc.Kind != DateTimeKind.Utc)
			throw new ArgumentException("GeneratedAtUtc must be UTC.", nameof(signal));
	}

	private static void ValidateSupportedSymbol(ITradingStrategy strategy, string symbol)
	{
		if (!strategy.Metadata.SupportedSymbols.Any(x => x.Equals(symbol, StringComparison.OrdinalIgnoreCase)))
			throw new InvalidOperationException($"Strategy '{strategy.Metadata.Name}' does not support symbol '{symbol}'.");
	}
}
