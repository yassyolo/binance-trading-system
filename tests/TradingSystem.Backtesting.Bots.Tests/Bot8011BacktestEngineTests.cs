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
        var signals = new[] { S(0, TradeSide.Long) };

        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with
            {
                EnterOnNextCandleOpen = true,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m
            },
            candles,
            signals);

        var entry = Assert.Single(result.Executions.Where(x => x.Type == "ENTRY"));
        Assert.Equal(Utc(1), entry.TimeUtc);
        Assert.True(entry.Price >= 110m);
    }

    [Fact]
    public async Task RunAsync_SameSideSignalWhileActive_IsBlocked()
    {
        var candles = Candles(4, 100m);
        var signals = new[]
        {
            S(0, TradeSide.Long),
            S(1, TradeSide.Long)
        };

        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m,
                CooldownSeconds = 0
            },
            candles,
            signals);

        Assert.Contains(result.Decisions, x =>
            x.Decision == "Block" &&
            x.Reason.Contains("ORDER_SIDE_LIMIT"));
    }

    [Fact]
    public async Task RunAsync_OppositeSignal_ClosesWithReverseReason()
    {
        var candles = Candles(4, 100m);
        var signals = new[]
        {
            S(0, TradeSide.Long),
            S(1, TradeSide.Short)
        };

        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 50m,
                InitialStopLossDistance = 50m,
                CooldownSeconds = 0
            },
            candles,
            signals);

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
            C(1, 100m, 120m, 80m, 100m)
        };
        var signals = new[] { S(0, TradeSide.Long) };

        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 10m,
                ConflictPolicy = IntrabarConflictPolicy.WorstCase,
                SlippageBasisPoints = 0m
            },
            candles,
            signals);

        var position = Assert.Single(result.Positions);
        Assert.Equal("INITIAL_SL", position.ExitReason);
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
        var signals = new[] { S(0, TradeSide.Long) };

        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 10m,
                TakeProfitCloseFraction = 0.5m,
                ConflictPolicy = IntrabarConflictPolicy.BestCase,
                SlippageBasisPoints = 0m,
                Stop3EntryOffset = 0m,
                Stop3TrailingStep = 1000m
            },
            candles,
            signals);

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
        var signals = new[] { S(0, TradeSide.Long) };

        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with
            {
                EnterOnNextCandleOpen = false,
                TakeProfitPercent = 10m,
                InitialStopLossDistance = 50m,
                TakeProfitCloseFraction = 0.5m,
                Stop3EntryOffset = 0m,
                Stop3TrailingStep = 1000m,
                SlippageBasisPoints = 0m
            },
            candles,
            signals);

        Assert.Contains(result.Executions, x => x.Reason == "TP_PARTIAL");
        Assert.Contains(result.Executions, x => x.Reason == "STOP3");

        var position = Assert.Single(result.Positions);
        Assert.True(position.PartialTakeProfitReached);
        Assert.Equal("STOP3", position.ExitReason);
    }

    [Fact]
    public async Task RunAsync_QuantityBelowMinimum_IsBlocked()
    {
        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with
            {
                Quantity = 0.0005m,
                MinimumQuantity = 0.001m,
                QuantityStep = 0.0001m,
                EnterOnNextCandleOpen = false
            },
            Candles(2, 100m),
            [S(0, TradeSide.Long)]);

        Assert.Empty(result.Positions);
        Assert.Contains(result.Decisions, x => x.Reason == "QUANTITY_OR_MARGIN_INVALID");
    }

    [Fact]
    public async Task RunAsync_LastCandlePendingSignal_DoesNotCreatePosition()
    {
        var candles = Candles(2, 100m);
        var signal = new HistoricalBotSignal(
            candles[^1].CloseTimeUtc,
            TradeSide.Long,
            "signal-last",
            "test");

        var result = await new Bot8011BacktestEngine().RunAsync(
            10_000m,
            Options() with { EnterOnNextCandleOpen = true },
            candles,
            [signal]);

        Assert.DoesNotContain(result.Executions, x => x.Type == "ENTRY");
        Assert.Contains(result.Decisions, x => x.Reason == "PENDING_NEXT_OPEN");
    }

    private static Bot8011BacktestOptions Options()
        => new()
        {
            Quantity = 0.002m,
            Leverage = 50,
            TakeProfitPercent = 10m,
            InitialStopLossDistance = 20m,
            TakeProfitCloseFraction = 0.5m,
            CooldownSeconds = 0,
            TakerFeeRate = 0m,
            SlippageBasisPoints = 0m,
            MinimumQuantity = 0.001m,
            QuantityStep = 0.001m,
            MinimumNotional = 1m,
            TickSize = 0.1m
        };

    private static HistoricalBotSignal S(int minute, TradeSide side)
        => new(Utc(minute), side, $"s-{minute}-{side}", "test");

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
