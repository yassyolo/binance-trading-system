using TradingSystem.Backtesting.Bots.Bot8011;
using TradingSystem.Backtesting.Bots.Bot8011.Models.Enums;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Bots.Models.Enums;
using TradingSystem.Domain.MarketData;
using Xunit;

namespace TradingSystem.Backtesting.Bots.Tests;

public sealed class Bot8011BacktestEngineTests
{
    [Fact]
    public async Task RunAsync_EnterOnNextCandleOpen_UsesNextOpen()
    {
        var candles = new[]
        {
            C(0, 100m, 101m, 99m, 100m),
            C(1, 110m, 111m, 109m, 110m),
            C(2, 110m, 111m, 109m, 110m)
        };

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = true,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m
            },
            candles,
            [S(0, TradeSide.Long)]);

        var entry = Assert.Single(result.Executions.Where(x => x.Type == "ENTRY"));

        Assert.Equal(Utc(1), entry.TimeUtc);
        Assert.Equal(110m, entry.Price);
    }

    [Fact]
    public async Task RunAsync_SameSideSignalWhileActive_IsBlocked()
    {
        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m,
                CooldownSeconds = 0
            },
            Candles(4, 100m),
            [
                S(0, TradeSide.Long),
                S(1, TradeSide.Long)
            ]);

        Assert.Contains(result.Decisions, x =>
            x.Decision == "Block" &&
            x.Reason.Contains("ORDER_SIDE_LIMIT"));
    }

    [Fact]
    public async Task RunAsync_OppositeSignal_ClosesWithReverseReason()
    {
        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m,
                CooldownSeconds = 0
            },
            Candles(4, 100m),
            [
                S(0, TradeSide.Long),
                S(1, TradeSide.Short)
            ]);

        Assert.Contains(result.Executions, x =>
            x.Type == "EXIT" &&
            x.Reason == "REVERSE_SIGNAL");
    }

    [Fact]
    public async Task RunAsync_WorstCase_WhenTpAndSlHitSameCandle_UsesInitialSl()
    {
        var candles = new[]
        {
            C(0, 100m, 100m, 100m, 100m),
            C(1, 100m, 120m, 80m, 100m),
            C(2, 100m, 100m, 100m, 100m)
        };

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 10m,
                ConflictPolicy = IntrabarConflictPolicy.WorstCase
            },
            candles,
            [S(0, TradeSide.Long)]);

        var position = Assert.Single(result.Positions);

        Assert.Equal("INITIAL_SL", position.ExitReason);
    }

    [Fact]
    public async Task RunAsync_StopLossFirst_WhenTpAndSlHitSameCandle_UsesInitialSl()
    {
        var candles = new[]
        {
            C(0, 100m, 100m, 100m, 100m),
            C(1, 100m, 120m, 80m, 100m),
            C(2, 100m, 100m, 100m, 100m)
        };

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 10m,
                ConflictPolicy = IntrabarConflictPolicy.StopLossFirst
            },
            candles,
            [S(0, TradeSide.Long)]);

        Assert.Equal("INITIAL_SL", Assert.Single(result.Positions).ExitReason);
    }

    [Fact]
    public async Task RunAsync_BestCase_WhenTpAndSlHitSameCandle_UsesTakeProfitPath()
    {
        var candles = new[]
        {
            C(0, 100m, 100m, 100m, 100m),
            C(1, 100m, 120m, 80m, 115m),
            C(2, 115m, 116m, 114m, 115m)
        };

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 10m,
                TakeProfitCloseFraction = 0.5m,
                ConflictPolicy = IntrabarConflictPolicy.BestCase,
                Stop3EntryOffset = 0m,
                Stop3TrailingStep = 1000m
            },
            candles,
            [S(0, TradeSide.Long)]);

        Assert.Contains(result.Executions, x => x.Reason == "TP_PARTIAL");
        Assert.True(Assert.Single(result.Positions).PartialTakeProfitReached);
    }

    [Fact]
    public async Task RunAsync_TakeProfitFirst_WhenTpAndSlHitSameCandle_UsesTakeProfitPath()
    {
        var candles = new[]
        {
            C(0, 100m, 100m, 100m, 100m),
            C(1, 100m, 120m, 80m, 115m),
            C(2, 115m, 116m, 114m, 115m)
        };

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 10m,
                ConflictPolicy = IntrabarConflictPolicy.TakeProfitFirst,
                Stop3TrailingStep = 1000m
            },
            candles,
            [S(0, TradeSide.Long)]);

        Assert.Contains(result.Executions, x => x.Reason == "TP_PARTIAL");
    }

    [Fact]
    public async Task RunAsync_TakeProfitPartial_LeavesRemainderForStop3()
    {
        var candles = new[]
        {
            C(0, 100m, 100m, 100m, 100m),
            C(1, 100m, 111m, 100m, 110m),
            C(2, 110m, 110m, 99m, 100m)
        };

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 50m,
                TakeProfitCloseFraction = 0.5m,
                Stop3EntryOffset = 0m,
                Stop3TrailingStep = 1000m
            },
            candles,
            [S(0, TradeSide.Long)]);

        Assert.Contains(result.Executions, x => x.Reason == "TP_PARTIAL");
        Assert.Contains(result.Executions, x => x.Reason == "STOP3");

        var position = Assert.Single(result.Positions);

        Assert.True(position.PartialTakeProfitReached);
        Assert.Equal("STOP3", position.ExitReason);
    }

    [Fact]
    public async Task RunAsync_TakeProfitFull_ClosesWithTpFullExit()
    {
        var candles = new[]
        {
            C(0, 100m, 100m, 100m, 100m),
            C(1, 100m, 111m, 100m, 110m),
            C(2, 110m, 110m, 109m, 110m)
        };

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 50m,
                TakeProfitCloseFraction = 1m
            },
            candles,
            [S(0, TradeSide.Long)]);

        var position = Assert.Single(result.Positions);

        Assert.Equal("TP_FULL_EXIT", position.ExitReason);
        Assert.True(position.PartialTakeProfitReached);
    }

    [Fact]
    public async Task RunAsync_QuantityBelowMinimum_IsBlocked()
    {
        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                Quantity = 0.0005m,
                MinimumQuantity = 0.001m,
                QuantityStep = 0.0001m,
                EnterOnNextCandleOpen = false
            },
            Candles(2, 100m),
            [S(0, TradeSide.Long)]);

        Assert.DoesNotContain(result.Executions, x => x.Type == "ENTRY");
        Assert.Contains(result.Decisions, x => x.Reason == "QUANTITY_OR_MARGIN_INVALID");
    }

    [Fact]
    public async Task RunAsync_NotionalBelowMinimum_IsBlocked()
    {
        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                MinimumNotional = 1m,
                EnterOnNextCandleOpen = false
            },
            Candles(2, 100m),
            [S(0, TradeSide.Long)]);

        Assert.DoesNotContain(result.Executions, x => x.Type == "ENTRY");
        Assert.Contains(result.Decisions, x => x.Reason == "QUANTITY_OR_MARGIN_INVALID");
    }

    [Fact]
    public async Task RunAsync_DisabledLong_IsBlocked()
    {
        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnableLong = false,
                EnterOnNextCandleOpen = false
            },
            Candles(2, 100m),
            [S(0, TradeSide.Long)]);

        Assert.Contains(result.Decisions, x => x.Reason == "SIDE_DISABLED");
        Assert.DoesNotContain(result.Executions, x => x.Type == "ENTRY");
    }

    [Fact]
    public async Task RunAsync_Cooldown_BlocksSecondSignal()
    {
        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                CooldownSeconds = 120,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m
            },
            Candles(4, 100m),
            [
                S(0, TradeSide.Long),
                S(1, TradeSide.Long)
            ]);

        Assert.Contains(result.Decisions, x => x.Reason == "SIGNAL_COOLDOWN");
    }

    [Fact]
    public async Task RunAsync_LastCandlePendingSignal_DoesNotCreatePosition()
    {
        var candles = Candles(2, 100m);
        var signal = new HistoricalBotSignal(
            candles[^1].OpenTimeUtc.AddSeconds(30),
            TradeSide.Long,
            "test",
            "signal-last");

        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with { EnterOnNextCandleOpen = true },
            candles,
            [signal]);

        Assert.DoesNotContain(result.Executions, x => x.Type == "ENTRY");
        Assert.Contains(result.Decisions, x => x.Reason == "PENDING_NEXT_OPEN");
    }

    [Fact]
    public async Task RunAsync_OpenPositionAtEnd_IsClosedWithBacktestEnd()
    {
        var result = await CreateSut().RunAsync(
            10_000m,
            CreateOptions() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m
            },
            Candles(2, 100m),
            [S(0, TradeSide.Long)]);

        Assert.Equal("BACKTEST_END", Assert.Single(result.Positions).ExitReason);
    }

    private static Bot8011BacktestEngine CreateSut() => new();

    private static Bot8011BacktestOptions CreateOptions()
        => new()
        {
            Quantity = 0.002m,
            Leverage = 50,
            TakeProfitPercent = 10m,
            InitialStopLossDistance = 20m,
            TakeProfitCloseFraction = 0.5m,
            CooldownSeconds = 0,
            EnableLong = true,
            EnableShort = true,
            Stop3EntryOffset = 0m,
            Stop3TrailingStep = 400m,
            Stop3TrailingBuffer = 50m,
            TakerFeeRate = 0m,
            SlippageBasisPoints = 0m,
            MinimumQuantity = 0.001m,
            QuantityStep = 0.001m,

            // Synthetic tests use prices around 100:
            // 100 * 0.002 = 0.20 notional.
            // Keep this below 0.20 so normal entry scenarios are valid.
            MinimumNotional = 0.1m,

            TickSize = 0.1m,
            EnterOnNextCandleOpen = false,
            ConflictPolicy = IntrabarConflictPolicy.WorstCase
        };

    private static HistoricalBotSignal S(int minute, TradeSide side)
        => new(
            Utc(minute).AddSeconds(30),
            side,
            "test",
            $"s-{minute}-{side}");

    private static MarketCandle[] Candles(int count, decimal price)
        => Enumerable.Range(0, count)
            .Select(i => C(i, price, price + 1m, price - 1m, price))
            .ToArray();

    private static MarketCandle C(
        int minute,
        decimal open,
        decimal high,
        decimal low,
        decimal close)
        => new(
            "BTCUSDC",
            "1m",
            Utc(minute),
            Utc(minute + 1),
            open,
            high,
            low,
            close,
            1m,
            true);

    private static DateTime Utc(int minute)
        => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(minute);
}
