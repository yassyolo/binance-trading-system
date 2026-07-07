using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8015;

public sealed class Bot8015Strategy(
    IOptions<Bot8015Options> options,
    ILogger<Bot8015Strategy> logger,
    TelegramNotificationService telegram)
    : ITradingStrategy
{
    private readonly Bot8015Options _options = options.Value;
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

        var oppositeSide = side == PositionSide.Long
            ? PositionSide.Short
            : PositionSide.Long;

        var oppositePositions = context.ActivePositions
            .Where(x => x.Side == oppositeSide)
            .ToList();

        if (oppositePositions.Count > 0)
        {
            _lastSignalAt[side] = DateTime.UtcNow;

            var positionsToClose = oppositePositions
    .Select(x => x.ShortId)
    .ToList();

            return StrategyDecision.OpenAfterClosing(
                side,
                positionsToClose,
                $"Close {oppositePositions.Count} opposite {oppositeSide} positions first");
        }

        var sameSideCount = context.ActivePositions.Count(x => x.Side == side);

        if (sameSideCount >= _options.OrderSideLimit)
        {
            await telegram.SendAsync(
                $"⏸️ BOT8015 {side} ignored. Limit reached: {sameSideCount}/{_options.OrderSideLimit}",
                cancellationToken);

            return StrategyDecision.Block(
                $"LIMIT_REACHED {sameSideCount}/{_options.OrderSideLimit}");
        }

        _lastSignalAt[side] = DateTime.UtcNow;

        logger.LogInformation(
            "BOT8015 decision OPEN. Side={Side}, ActiveSameSide={SameSideCount}",
            side,
            sameSideCount);

        return StrategyDecision.Open(side, "OK");
    }

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var lastSignalAt))
            return false;

        return DateTime.UtcNow - lastSignalAt <
               TimeSpan.FromSeconds(_options.CooldownSeconds);
    }
}