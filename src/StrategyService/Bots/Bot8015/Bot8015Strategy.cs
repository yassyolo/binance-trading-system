using Microsoft.Extensions.Options;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015Strategy(
	IOptions<Bot8015Options> options,
	ILogger<Bot8015Strategy> logger)
	: ITradingStrategy, IHasSignalCooldown
{
	private readonly Bot8015Options _options = options.Value;

	public StrategyMetadata Metadata =>
		new(
			_options.BotName,
			_options.StrategyVersion,
			PositionMode.Hedge,
			[_options.Symbol]);

	public TimeSpan SignalCooldown =>
		TimeSpan.FromSeconds(_options.CooldownSeconds);

	public Task<StrategyDecision> DecideAsync(
		StrategyContext context,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var side = context.Signal.Side;

		logger.LogInformation(
			"BOT8015 evaluating signal. Symbol = {Symbol}, Side = {Side}, " +
			"Source = {Source}, ActivePositions = {ActivePositions}",
			context.Signal.Symbol,
			side,
			context.Signal.Source,
			context.ActivePositions.Count);

		if (side == PositionSide.Long && !_options.EnableLong)
		{
			logger.LogInformation(
				"BOT8015 blocked LONG because LONG is disabled.");

			return Task.FromResult(
				StrategyDecision.Block(
					side,
					"LONG is disabled."));
		}

		if (side == PositionSide.Short && !_options.EnableShort)
		{
			logger.LogInformation(
				"BOT8015 blocked SHORT because SHORT is disabled.");

			return Task.FromResult(
				StrategyDecision.Block(
					side,
					"SHORT is disabled."));
		}

		var oppositePositionIds = context.ActivePositions
			.Where(position => position.Side != side)
			.Select(position => position.ShortId)
			.ToArray();

		if (oppositePositionIds.Length > 0)
		{
			logger.LogInformation(
				"BOT8015 reverse signal will close {Count} opposite positions. Side = {Side}",
				oppositePositionIds.Length,
				side);

			return Task.FromResult(
				StrategyDecision.OpenAfterClosing(
					side,
					oppositePositionIds,
					$"Reverse signal: close {oppositePositionIds.Length} opposite position(s)."));
		}

		var sameSidePositionsCount = context.ActivePositions.Count(
			position => position.Side == side);

		if (sameSidePositionsCount >= _options.OrderSideLimit)
		{
			logger.LogInformation(
				"BOT8015 signal blocked by side limit. Side = {Side}, Count = {Count}, Limit = {Limit}",
				side,
				sameSidePositionsCount,
				_options.OrderSideLimit);

			return Task.FromResult(
				StrategyDecision.Block(
					side,
					$"ORDER_SIDE_LIMIT reached ({sameSidePositionsCount}/{_options.OrderSideLimit})."));
		}

		logger.LogInformation(
			"BOT8015 accepted signal. Symbol = {Symbol}, Side = {Side}",
			context.Signal.Symbol,
			side);

		return Task.FromResult(
			StrategyDecision.Open(
				side,
				$"BOT8015 accepted {side} signal."));
	}
}