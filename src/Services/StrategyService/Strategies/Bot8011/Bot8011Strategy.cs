using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8011;

public sealed class Bot8011Strategy(
        IOptions<Bot8011Options> options,
        Bot8011EffectivePositionService effectivePositions,
        TelegramNotificationService telegram,
        ILogger<Bot8011Strategy> logger) : ITradingStrategy
{
    private readonly Bot8011Options options = options.Value;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = [];

    public string BotName => options.BotName;

    public async Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken)
    {
        var side = context.Signal.Side;

        if (side == PositionSide.Long && !options.EnableLong)
            return StrategyDecision.Block("LONG disabled");

        if (side == PositionSide.Short && !options.EnableShort)
            return StrategyDecision.Block("SHORT disabled");

        if (IsInCooldown(side, out var remainingSeconds))
        {
            await telegram.SendAsync(
                $"⏸️ BOT8011 {side} signal ignored. Cooldown: {remainingSeconds}s",
                cancellationToken);

            return StrategyDecision.Block($"{side} cooldown active: {remainingSeconds}s remaining");
        }

        var effectiveActive = await effectivePositions.GetEffectiveActiveAsync(cancellationToken);

        var sameSide = effectiveActive
            .Where(x => x.Side == side)
            .ToList();

        var oppositeSide = side == PositionSide.Long
            ? PositionSide.Short
            : PositionSide.Long;

        var opposite = effectiveActive
            .Where(x => x.Side == oppositeSide)
            .ToList();

        if (opposite.Count > 0)
        {
            _lastSignalAt[side] = DateTime.UtcNow;

            var idsToClose = opposite.Select(x => x.ShortId).ToList();

            logger.LogInformation(
                "BOT8011 opposite positions found. Side={Side}, OppositeCount={Count}",
                side,
                opposite.Count);

            return StrategyDecision.OpenAfterClosing(
                side,
                idsToClose,
                $"Close {opposite.Count} opposite {oppositeSide} position(s), then open {side}");
        }

        if (sameSide.Count >= options.OrderSideLimit)
        {
            await telegram.SendAsync(
                $"⏸️ BOT8011 {side} signal ignored. Limit reached: {sameSide.Count}/{options.OrderSideLimit}",
                cancellationToken);

            return StrategyDecision.Block(
                $"ORDER_SIDE_LIMIT reached ({sameSide.Count}/{options.OrderSideLimit})");
        }

        _lastSignalAt[side] = DateTime.UtcNow;

        return StrategyDecision.Open(
            side,
            $"OK BOT8011 open {side}. Mark={context.MarkPrice}");
    }

    private bool IsInCooldown(PositionSide side, out int remainingSeconds)
    {
        remainingSeconds = 0;

        if (!_lastSignalAt.TryGetValue(side, out var lastSignalAt))
            return false;

        var elapsed = DateTime.UtcNow - lastSignalAt;
        var cooldown = TimeSpan.FromSeconds(options.CooldownSeconds);

        if (elapsed >= cooldown)
            return false;

        remainingSeconds = (int)(cooldown - elapsed).TotalSeconds;
        return true;
    }
}