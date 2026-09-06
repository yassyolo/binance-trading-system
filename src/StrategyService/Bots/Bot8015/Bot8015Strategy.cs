using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8015.Configuration;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8015;

public sealed class Bot8015Strategy(
	IOptions<Bot8015Options> options)
	: ITradingStrategy, 
	IHasSignalCooldown
{
	private readonly Bot8015Options _options = options.Value;

	public StrategyMetadata Metadata => new(_options.BotName, _options.StrategyVersion, PositionMode.Hedge, [_options.Symbol]);

	public TimeSpan SignalCooldown => TimeSpan.FromSeconds(_options.CooldownSeconds);

	public Task<StrategyDecision> DecideAsync(StrategyContext ctx, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var side = ctx.Signal.Side;

		if (side == PositionSide.Long && !_options.EnableLong)
			return Task.FromResult(StrategyDecision.Block(side, "LONG is disabled."));

		if (side == PositionSide.Short && !_options.EnableShort)
			return Task.FromResult(StrategyDecision.Block(side, "SHORT is disabled."));

		var oppositePositionIds = ctx.ActivePositions.Where(p => p.Side != side).Select(p => p.ShortId).ToArray();
		if (oppositePositionIds.Length > 0)
			return Task.FromResult(StrategyDecision.OpenAfterClosing(side, oppositePositionIds, $"Reverse signal: close {oppositePositionIds.Length} opposite p(s)."));

		var sameSidePositionsCount = ctx.ActivePositions.Count(p => p.Side == side);
        return Task.FromResult(sameSidePositionsCount >= _options.OrderSideLimit
           ? StrategyDecision.Block(side, $"ORDER_SIDE_LIMIT reached ({sameSidePositionsCount}/{_options.OrderSideLimit}).")
           : StrategyDecision.Open(side, $"BOT8015 accepted {side} signal."));
    }
}