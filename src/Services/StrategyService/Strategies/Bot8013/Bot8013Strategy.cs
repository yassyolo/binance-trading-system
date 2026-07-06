using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8013;

public sealed class Bot8013Strategy(
    IOptions<Bot8013Options> options,
    Bot8013GapPolicy gapPolicy,
    ILogger<Bot8013Strategy> logger,
    TelegramNotificationService telegram)
    : ITradingStrategy
{
    private readonly Bot8013Options _options = options.Value;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = [];

    public string BotName => _options.BotName;

    public async Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken)
    {
        var side = context.Signal.Side;

        if (side == PositionSide.Long && !_options.EnableLong)
            return StrategyDecision.Block("LONG disabled");

        if (side == PositionSide.Short && !_options.EnableShort)
            return StrategyDecision.Block("SHORT disabled");

        if (IsInCooldown(side))
            return StrategyDecision.Block($"{side} cooldown active");

        var decision = gapPolicy.Validate(
            side,
            context.MarkPrice,
            context.ActivePositions);

        logger.LogInformation(
            "BOT8013 BEFORE_OPEN {Side}: Mark={MarkPrice}, ShouldOpen={ShouldOpen}, Reason={Reason}",
            side,
            Math.Round(context.MarkPrice, 0),
            decision.ShouldOpen,
            decision.Reason);

        if (!decision.ShouldOpen)
        {
            await telegram.SendAsync(
                $"⏸️ BOT8013 {side} blocked: {decision.Reason}",
                cancellationToken);

            return decision;
        }

        _lastSignalAt[side] = DateTime.UtcNow;
        return decision;
    }

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var lastSignalAt))
            return false;

        return DateTime.UtcNow - lastSignalAt <
               TimeSpan.FromSeconds(_options.CooldownSeconds);
    }
}