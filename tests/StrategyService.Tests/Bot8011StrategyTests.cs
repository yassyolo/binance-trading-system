using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011;
using StrategyService.Bots.Bot8011.Configuration;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Application.Strategies.Models.Enums;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;
using Xunit;

namespace StrategyService.Tests;

public sealed class Bot8011StrategyTests
{
    [Fact]
    public async Task DecideAsync_LongDisabled_Blocks()
    {
        var sut = CreateStrategy(new Bot8011Options { EnableLong = false });

        var result = await sut.DecideAsync(Context(PositionSide.Long), default);

        Assert.Equal(StrategyDecisionType.Ignore, result.Type);
        Assert.False(result.ShouldOpen);
        Assert.Contains("LONG is disabled", result.Reason);
    }

    [Fact]
    public async Task DecideAsync_ShortDisabled_Blocks()
    {
        var sut = CreateStrategy(new Bot8011Options { EnableShort = false });

        var result = await sut.DecideAsync(Context(PositionSide.Short), default);

        Assert.Equal(StrategyDecisionType.Ignore, result.Type);
        Assert.False(result.ShouldOpen);
        Assert.Contains("SHORT is disabled", result.Reason);
    }

    [Fact]
    public async Task DecideAsync_OppositePosition_ReturnsOpenAfterClosing()
    {
        var sut = CreateStrategy();
        var active = new[]
        {
            Position("short-1", PositionSide.Short),
            Position("short-2", PositionSide.Short)
        };

        var result = await sut.DecideAsync(Context(PositionSide.Long, active), default);

        Assert.Equal(StrategyDecisionType.OpenAfterClosing, result.Type);
        Assert.True(result.ShouldOpen);
        Assert.True(result.ShouldClosePositions);
        Assert.Equal(["short-1", "short-2"], result.PositionsToClose.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task DecideAsync_SameSideAtLimit_Blocks()
    {
        var sut = CreateStrategy(new Bot8011Options { OrderSideLimit = 1 });

        var result = await sut.DecideAsync(
            Context(PositionSide.Long, [Position("long-1", PositionSide.Long)]),
            default);

        Assert.False(result.ShouldOpen);
        Assert.Contains("ORDER_SIDE_LIMIT", result.Reason);
    }

    [Fact]
    public async Task DecideAsync_SameSideBelowLimit_Opens()
    {
        var sut = CreateStrategy(new Bot8011Options { OrderSideLimit = 2 });

        var result = await sut.DecideAsync(
            Context(PositionSide.Long, [Position("long-1", PositionSide.Long)]),
            default);

        Assert.Equal(StrategyDecisionType.Open, result.Type);
        Assert.True(result.ShouldOpen);
    }

    [Fact]
    public void Metadata_UsesHedgeModeAndConfiguredValues()
    {
        var sut = CreateStrategy(new Bot8011Options
        {
            BotName = "BOTX",
            StrategyVersion = "2.3.4",
            Symbol = "ETHUSDC"
        });

        Assert.Equal("BOTX", sut.Metadata.Name);
        Assert.Equal("2.3.4", sut.Metadata.Version);
        Assert.Equal(PositionMode.Hedge, sut.Metadata.PositionMode);
        Assert.Contains("ETHUSDC", sut.Metadata.SupportedSymbols);
    }

    [Fact]
    public void SignalCooldown_UsesConfiguredSeconds()
    {
        var sut = CreateStrategy(new Bot8011Options { CooldownSeconds = 42 });

        Assert.Equal(TimeSpan.FromSeconds(42), sut.SignalCooldown);
    }

    private static Bot8011Strategy CreateStrategy(Bot8011Options? options = null)
        => new(Options.Create(options ?? new Bot8011Options()));

    private static StrategyContext Context(
        PositionSide side,
        IReadOnlyCollection<ActivePositionView>? active = null)
        => new()
        {
            Signal = new TradeSignal
            {
                SignalId = "signal-1",
                BotName = "BOT8011",
                Symbol = "BTCUSDC",
                Side = side,
                Source = "test",
                GeneratedAtUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            },
            MarkPrice = 60_000m,
            ActivePositions = active ?? [],
            EvaluatedAtUtc = new DateTime(2026, 1, 1, 12, 0, 1, DateTimeKind.Utc)
        };

    private static ActivePositionView Position(string id, PositionSide side)
        => new()
        {
            ShortId = id,
            BotName = "BOT8011",
            Symbol = "BTCUSDC",
            Side = side,
            EntryPrice = 60_000m,
            Quantity = 0.002m,
            RemainingQuantity = 0.002m,
            CreatedAtUtc = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        };
}
