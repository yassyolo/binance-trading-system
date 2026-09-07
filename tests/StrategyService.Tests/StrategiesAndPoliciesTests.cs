using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8011;
using StrategyService.Bots.Bot8011.Configuration;
using StrategyService.Bots.Bot8013;
using StrategyService.Bots.Bot8013.Configuration;
using StrategyService.Bots.Bot8015;
using StrategyService.Bots.Bot8015.Configuration;
using StrategyService.Bots.Bot8016;
using StrategyService.Bots.Bot8016.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Positions.Models;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Application.Strategies.Models.Enums;
using TradingSystem.BotRuntime.Configuration.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Signals;
using TradingSystem.Strategies.Alligator;
using TradingSystem.Strategies.Alligator.Models;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Grid.Models;
using TradingSystem.Strategies.Protection;
using TradingSystem.Strategies.Protection.Models;
using Xunit;

namespace StrategyService.Tests;

public sealed class StrategiesAndPoliciesTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GridSpacing_UsesNewestSameSidePosition()
    {
        var sut = new GridSpacingPolicy();
        var positions = new[]
        {
            new GridPositionReference(PositionSide.Long, 50_200m, Now.AddMinutes(-10)),
            new GridPositionReference(PositionSide.Long, 51_000m, Now.AddMinutes(-1))
        };

        var result = sut.Evaluate(PositionSide.Long, 50_300m, positions, new(400m, 200m, 3));

        Assert.True(result.Allowed);
        Assert.Contains("50400", result.Reason.Replace(" ", ""));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GridSpacing_NonPositiveMarkPrice_Blocks(decimal markPrice)
    {
        var result = new GridSpacingPolicy().Evaluate(PositionSide.Long, markPrice, [], new(400m, 200m, 2));

        Assert.False(result.Allowed);
        Assert.Contains("greater than zero", result.Reason);
    }

    [Fact]
    public void GridSpacing_InvalidNewestTakeProfit_Blocks()
    {
        var positions = new[]
        {
            new GridPositionReference(PositionSide.Long, 0m, Now)
        };

        var result = new GridSpacingPolicy().Evaluate(PositionSide.Long, 50_000m, positions, new(400m, 200m, 2));

        Assert.False(result.Allowed);
        Assert.Contains("invalid take-profit", result.Reason);
    }

    [Fact]
    public void GridSpacing_NegativeDistance_Blocks()
    {
        var result = new GridSpacingPolicy().Evaluate(PositionSide.Long, 50_000m, [], new(-1m, 200m, 2));

        Assert.False(result.Allowed);
        Assert.Contains("cannot be negative", result.Reason);
    }

    [Fact]
    public void Alligator_MinimumRangeExactBoundary_AllowsLong()
    {
        var input = new AlligatorEntryInput("BTCUSDC", "5m", true, 100m, 150m, 100m, 140m, 120m, 140m);
        var parameters = new AlligatorEntryParameters("BTCUSDC", "5m", true, true, true, 50m);

        var result = new AlligatorEntryPolicy().Evaluate(input, parameters);

        Assert.NotNull(result);
        Assert.Equal(PositionSide.Long, result.Side);
    }

    [Fact]
    public void Alligator_LongAtSmaEquality_PassesFilter()
    {
        var input = new AlligatorEntryInput("BTCUSDC", "5m", true, 100m, 180m, 90m, 150m, 120m, 150m);
        var parameters = new AlligatorEntryParameters("BTCUSDC", "5m", true, true, true, 1m);

        var result = new AlligatorEntryPolicy().Evaluate(input, parameters);

        Assert.NotNull(result);
        Assert.Equal(PositionSide.Long, result.Side);
    }

    [Fact]
    public void Alligator_ShortAtSmaEquality_PassesFilter()
    {
        var input = new AlligatorEntryInput("BTCUSDC", "5m", true, 160m, 180m, 90m, 120m, 140m, 120m);
        var parameters = new AlligatorEntryParameters("BTCUSDC", "5m", true, true, true, 1m);

        var result = new AlligatorEntryPolicy().Evaluate(input, parameters);

        Assert.NotNull(result);
        Assert.Equal(PositionSide.Short, result.Side);
    }

    [Fact]
    public void Alligator_BullishCandleWithoutTeethCross_ReturnsNull()
    {
        var input = new AlligatorEntryInput("BTCUSDC", "5m", true, 130m, 180m, 90m, 160m, 120m, 150m);

        var result = new AlligatorEntryPolicy().Evaluate(
            input,
            new AlligatorEntryParameters("BTCUSDC", "5m", true, true, false, 1m));

        Assert.Null(result);
    }

    [Fact]
    public void Stop3_InitialTrigger_IsSymmetricAroundEntry()
    {
        var sut = new Stop3Policy();
        var parameters = new Stop3Parameters(25m, 400m, 50m);

        Assert.Equal(50_025m, sut.InitialTrigger(PositionSide.Long, 50_000m, parameters));
        Assert.Equal(49_975m, sut.InitialTrigger(PositionSide.Short, 50_000m, parameters));
    }

    [Fact]
    public void Stop3_LongAtExactTrailingStep_MovesByOneBuffer()
    {
        var result = new Stop3Policy().NextTrigger(
            PositionSide.Long,
            50_400m,
            50_000m,
            new Stop3Parameters(0m, 400m, 50m));

        Assert.Equal(50_050m, result);
    }

    [Fact]
    public void Stop3_LongBelowTrailingStep_DoesNotMove()
    {
        var result = new Stop3Policy().NextTrigger(
            PositionSide.Long,
            50_399m,
            50_000m,
            new Stop3Parameters(0m, 400m, 50m));

        Assert.Equal(50_000m, result);
    }

    [Fact]
    public void Stop3_ShortAtExactTrailingStep_MovesByOneBuffer()
    {
        var result = new Stop3Policy().NextTrigger(
            PositionSide.Short,
            49_600m,
            50_000m,
            new Stop3Parameters(0m, 400m, 50m));

        Assert.Equal(49_950m, result);
    }

    [Fact]
    public void Stop3_LargeMove_StillMovesOnlyOneBufferPerEvaluation()
    {
        var result = new Stop3Policy().NextTrigger(
            PositionSide.Long,
            52_000m,
            50_000m,
            new Stop3Parameters(0m, 400m, 50m));

        Assert.Equal(50_050m, result);
    }

    [Theory]
    [InlineData(PositionSide.Long, 101, 100, 90, true)]
    [InlineData(PositionSide.Long, 100, 100, 90, false)]
    [InlineData(PositionSide.Short, 89, 100, 90, true)]
    [InlineData(PositionSide.Short, 90, 100, 90, false)]
    public void Stop3_Breakout_IsStrict(PositionSide side, decimal close, decimal high, decimal low, bool expected)
    {
        Assert.Equal(expected, new Stop3Policy().Breakout(side, close, high, low));
    }

    [Theory]
    [InlineData(PositionSide.Long, 99, 100, true)]
    [InlineData(PositionSide.Long, 100, 100, false)]
    [InlineData(PositionSide.Short, 101, 100, true)]
    [InlineData(PositionSide.Short, 100, 100, false)]
    public void Stop3_TeethExit_IsStrict(PositionSide side, decimal close, decimal teeth, bool expected)
    {
        Assert.Equal(expected, new Stop3Policy().TeethExit(side, close, teeth));
    }

    [Fact]
    public async Task Bot8011_OppositePositions_ReturnsOpenAfterClosing()
    {
        var sut = new Bot8011Strategy(Options.Create(new Bot8011Options()));

        var result = await sut.DecideAsync(
            Context(PositionSide.Long, Position("short-1", PositionSide.Short), Position("short-2", PositionSide.Short)),
            default);

        Assert.Equal(StrategyDecisionType.OpenAfterClosing, result.Type);
        Assert.True(result.ShouldOpen);
        Assert.True(result.ShouldClosePositions);
        Assert.Equal(["short-1", "short-2"], result.PositionsToClose);
    }

    [Fact]
    public async Task Bot8011_SameSideLimitReached_Blocks()
    {
        var options = new Bot8011Options { OrderSideLimit = 1 };
        var sut = new Bot8011Strategy(Options.Create(options));

        var result = await sut.DecideAsync(Context(PositionSide.Long, Position("long-1", PositionSide.Long)), default);

        Assert.Equal(StrategyDecisionType.Ignore, result.Type);
        Assert.Contains("ORDER_SIDE_LIMIT", result.Reason);
    }

    [Fact]
    public async Task Bot8015_DisabledShort_BlocksBeforePositionLogic()
    {
        var options = new Bot8015Options { EnableShort = false };
        var sut = new Bot8015Strategy(Options.Create(options));

        var result = await sut.DecideAsync(
            Context(PositionSide.Short, Position("long-1", PositionSide.Long)),
            default);

        Assert.False(result.ShouldOpen);
        Assert.Equal("SHORT is disabled.", result.Reason);
    }

    [Fact]
    public async Task Bot8016_RuntimeSideLimit_OverridesStaticPositionSideLimit()
    {
        var options = new Bot8016Options { PositionSideLimit = 3 };
        var sut = new Bot8016Strategy(Options.Create(options), NullLogger<Bot8016Strategy>.Instance);
        var runtime = Runtime(orderSideLimit: 1);

        var result = await sut.DecideAsync(
            Context(PositionSide.Long, Position("long-1", PositionSide.Long)) with { RuntimeConfiguration = runtime },
            default);

        Assert.False(result.ShouldOpen);
        Assert.Contains("(1/1)", result.Reason);
    }

    [Fact]
    public async Task Bot8016_UsesSignalReasonMetadata_WhenAccepted()
    {
        var sut = new Bot8016Strategy(
            Options.Create(new Bot8016Options { PositionSideLimit = 2 }),
            NullLogger<Bot8016Strategy>.Instance);

        var context = Context(PositionSide.Long) with
        {
            Signal = Signal(PositionSide.Long) with
            {
                Metadata = new Dictionary<string, string> { ["reason"] = "Bullish Teeth cross." }
            }
        };

        var result = await sut.DecideAsync(context, default);

        Assert.True(result.ShouldOpen);
        Assert.Equal("Bullish Teeth cross.", result.Reason);
    }

    [Fact]
    public async Task Bot8013_RuntimeGridOverrides_AreUsed()
    {
        var options = new Bot8013Options
        {
            PriceDistance = 400m,
            ProfitDistance = 200m,
            OrderSideLimit = 2
        };
        var gap = new TpOnlyGridGapPolicy<Bot8013Options>(options, new GridSpacingPolicy());
        var sut = new Bot8013Strategy(Options.Create(options), gap);

        var context = Context(PositionSide.Long, Position("long-1", PositionSide.Long, tp: 50_200m)) with
        {
            MarkPrice = 49_900m,
            RuntimeConfiguration = Runtime(priceDistance: 100m, profitDistance: 100m, orderSideLimit: 2)
        };

        var result = await sut.DecideAsync(context, default);

        Assert.True(result.ShouldOpen);
        Assert.Contains("requiredMaximum = 50000", result.Reason);
    }

    private static StrategyContext Context(PositionSide side, params ActivePositionView[] positions)
        => new()
        {
            Signal = Signal(side),
            MarkPrice = 50_000m,
            ActivePositions = positions,
            EvaluatedAtUtc = Now
        };

    private static TradeSignal Signal(PositionSide side)
        => new()
        {
            SignalId = "signal-1",
            BotName = "BOT",
            Symbol = "BTCUSDC",
            Side = side,
            Source = "test",
            GeneratedAtUtc = Now
        };

    private static ActivePositionView Position(string id, PositionSide side, decimal? tp = 50_200m)
        => new()
        {
            ShortId = id,
            BotName = "BOT",
            Symbol = "BTCUSDC",
            Side = side,
            EntryPrice = 50_000m,
            Quantity = 0.002m,
            RemainingQuantity = 0.002m,
            TpPrice = tp,
            CreatedAtUtc = Now
        };

    private static BotRuntimeConfiguration Runtime(
        decimal? priceDistance = null,
        decimal? profitDistance = null,
        int? orderSideLimit = null)
        => new(
            "BOT",
            "Grid",
            "BTCUSDC",
            "Paper",
            "Internal",
            true,
            true,
            0.002m,
            50,
            priceDistance,
            profitDistance,
            orderSideLimit,
            180,
            1,
            Now,
            false);
}
