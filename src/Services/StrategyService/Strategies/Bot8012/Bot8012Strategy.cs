using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8012;

public sealed class Bot8012Strategy(
        IOptions<Bot8012Options> options,
        Bot8012GapPolicy gapPolicy,
        ILogger<Bot8012Strategy> logger,
        TelegramNotificationService telegram)
    : ITradingStrategy
{
    private readonly Bot8012Options options = options.Value;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = [];

    public string BotName => options.BotName;

    public async Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken)
    {
        var side = context.Signal.Side;

        if (side == PositionSide.Long && !options.EnableLong)
            return await Task.FromResult(StrategyDecision.Block("LONG disabled"));

        if (side == PositionSide.Short && !options.EnableShort)
            return await Task.FromResult(StrategyDecision.Block("SHORT disabled"));

        if (IsInCooldown(side))
            return  await Task.FromResult(StrategyDecision.Block($"{side} cooldown active"));

        var decision = gapPolicy.Validate(
            side,
            context.MarkPrice,
            context.ActivePositions);

        if (!decision.ShouldOpen)
        {
            await telegram.SendAsync(
                $"⛔ BOT8012 BLOCKED\nSide: {side}\nMark: {context.MarkPrice}\nReason: {decision.Reason}",
                cancellationToken);
        }

        if (decision.ShouldOpen)
            _lastSignalAt[side] = DateTime.UtcNow;

        logger.LogInformation(
            "BOT8012 decision. Side={Side}, ShouldOpen={ShouldOpen}, Reason={Reason}",
            side,
            decision.ShouldOpen,
            decision.Reason);

        return await Task.FromResult(decision);
    }

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var lastSignalAt))
            return false;

        return DateTime.UtcNow - lastSignalAt <
               TimeSpan.FromSeconds(options.CooldownSeconds);
    }
}