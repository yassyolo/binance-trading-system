using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Enums;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016Strategy(IOptions<Bot8016Options> options, ILogger<Bot8016Strategy> logger) : ITradingStrategy
{
    private readonly Bot8016Options _options = options.Value;

    public StrategyMetadata Metadata => new(_options.BotName, _options.StrategyVersion, PositionMode.Stop3, [_options.Symbol],
        "bot8016.alligator", "BOT8016 Alligator", "Alligator/MA200 entry with BOT8016 protection lifecycle.");

    public Task<StrategyDecision> DecideAsync(StrategyContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        var side = ctx.Signal.Side;
        
        if (side == PositionSide.Long && !_options.EnableLong)
            return Task.FromResult(StrategyDecision.Block(side, "LONG is disabled."));
       
        if (side == PositionSide.Short && !_options.EnableShort)
            return Task.FromResult(StrategyDecision.Block(side, "SHORT is disabled."));

        var sideLimit = ctx.RuntimeConfiguration?.OrderSideLimit ?? _options.PositionSideLimit;     
        var sameSideCount = ctx.ActivePositions.Count(x => x.Side == side);
        if (sameSideCount >= sideLimit)
            return Task.FromResult(StrategyDecision.Block(side, $"ORDER_SIDE_LIMIT reached ({sameSideCount}/{sideLimit})."));

        logger.LogInformation("BOT8016 accepted pre-evaluated Alligator signal. Side = {Side}, ActiveSameSide = {Count}, Limit = {Limit}", side, sameSideCount, sideLimit);
        
        return Task.FromResult(StrategyDecision.Open(side,
            ctx.Signal.Metadata.TryGetValue("reason", out var reason) && !string.IsNullOrWhiteSpace(reason)
                ? reason
                : "BOT8016 Alligator entry accepted."));
    }
}
