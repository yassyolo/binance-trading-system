using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Services;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8011;

public sealed class Bot8011Strategy(
    IOptions<Bot8011Options> options,
    TelegramNotificationService telegram,
    ILogger<Bot8011Strategy> logger)
    : ITradingStrategy, IHasSignalCooldown
{
    private readonly Bot8011Options _options = options.Value;

    public string BotName => _options.BotName;

    public StrategyMetadata Metadata => new(
        Name: _options.BotName,
        Version: _options.StrategyVersion,
        PositionMode: PositionMode.Hedge,
        SupportedSymbols: [_options.Symbol]);

    public TimeSpan SignalCooldown =>
        TimeSpan.FromSeconds(_options.CooldownSeconds);

    public async Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken)
    {
        var side = context.Signal.Side;

        if (side == PositionSide.Long && !_options.EnableLong)
        {
            return StrategyDecision.Block(
                side,
                "LONG disabled");
        }

        if (side == PositionSide.Short && !_options.EnableShort)
        {
            return StrategyDecision.Block(
                side,
                "SHORT disabled");
        }

        var sameSidePositions = context.ActivePositions
            .Where(x => x.Side == side)
            .ToList();

        var oppositeSide = side == PositionSide.Long
            ? PositionSide.Short
            : PositionSide.Long;

        var oppositePositions = context.ActivePositions
            .Where(x => x.Side == oppositeSide)
            .ToList();

        if (oppositePositions.Count > 0)
        {
            var positionIdsToClose = oppositePositions
                .Select(x => x.ShortId)
                .ToArray();

            logger.LogInformation(
                "BOT8011 reverse signal. NewSide={NewSide}, OppositeSide={OppositeSide}, OppositeCount={OppositeCount}",
                side,
                oppositeSide,
                oppositePositions.Count);

            return StrategyDecision.OpenAfterClosing(
                side,
                positionIdsToClose,
                $"Close {oppositePositions.Count} opposite {oppositeSide} position(s), then open {side}");
        }

        if (sameSidePositions.Count >= _options.OrderSideLimit)
        {
            await telegram.SendAsync(
                $"⏸️ {_options.BotName} {side} ignored. " +
                $"Limit: {sameSidePositions.Count}/{_options.OrderSideLimit}",
                cancellationToken);

            return StrategyDecision.Block(
                side,
                $"ORDER_SIDE_LIMIT reached ({sameSidePositions.Count}/{_options.OrderSideLimit})");
        }

        logger.LogInformation(
            "BOT8011 signal accepted. Side={Side}, MarkPrice={MarkPrice}, ActiveSameSide={ActiveSameSide}",
            side,
            context.MarkPrice,
            sameSidePositions.Count);

        return StrategyDecision.Open(
            side,
            $"BOT8011 open {side}. Mark={context.MarkPrice}");
    }
}