using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8012;

public sealed class Bot8012Strategy : ITradingStrategy
{
    private readonly Bot8012Options _options;
    private readonly Bot8012GapPolicy _gapPolicy;
    private readonly ILogger<Bot8012Strategy> _logger;
    private readonly Dictionary<PositionSide, DateTime> _lastSignalAt = [];

    public Bot8012Strategy(
        IOptions<Bot8012Options> options,
        Bot8012GapPolicy gapPolicy,
        ILogger<Bot8012Strategy> logger)
    {
        _options = options.Value;
        _gapPolicy = gapPolicy;
        _logger = logger;
    }

    public string BotName => _options.BotName;

    public Task<StrategyDecision> DecideAsync(
        StrategyContext context,
        CancellationToken cancellationToken)
    {
        var side = context.Signal.Side;

        if (side == PositionSide.Long && !_options.EnableLong)
            return Task.FromResult(StrategyDecision.Block("LONG disabled"));

        if (side == PositionSide.Short && !_options.EnableShort)
            return Task.FromResult(StrategyDecision.Block("SHORT disabled"));

        if (IsInCooldown(side))
            return Task.FromResult(StrategyDecision.Block($"{side} cooldown active"));

        var decision = _gapPolicy.Validate(
            side,
            context.MarkPrice,
            context.ActivePositions);

        if (decision.ShouldOpen)
            _lastSignalAt[side] = DateTime.UtcNow;

        _logger.LogInformation(
            "BOT8012 decision. Side={Side}, ShouldOpen={ShouldOpen}, Reason={Reason}",
            side,
            decision.ShouldOpen,
            decision.Reason);

        return Task.FromResult(decision);
    }

    private bool IsInCooldown(PositionSide side)
    {
        if (!_lastSignalAt.TryGetValue(side, out var lastSignalAt))
            return false;

        return DateTime.UtcNow - lastSignalAt <
               TimeSpan.FromSeconds(_options.CooldownSeconds);
    }
}