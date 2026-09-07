using TradingSystem.Backtesting.Bots.Bot8012;
using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Bots.Models.Enums;
using TradingSystem.Domain.MarketData;
using TradingSystem.Strategies.Grid;
using Xunit;

namespace TradingSystem.Backtesting.Bots.Tests;

public sealed class Bot8012BacktestEngineTests
{
    [Fact]
    public void Run_FirstLongSignal_ClosesAtTakeProfit()
    {
        var result = CreateSut().Run(
            [
                C(0, 100m, 101m, 99m, 100m),
                C(1, 100m, 111m, 99m, 110m)
            ],
            [S(0, TradeSide.Long)],
            CreateOptions() with { ProfitDistance = 10m });

        var position = Assert.Single(result.Positions);

        Assert.Equal("TAKE_PROFIT", position.ExitReason);
        Assert.Equal(10m, position.GrossPnl);
    }

    [Fact]
    public void Run_LongGapExactBoundary_IsAllowed()
    {
        var result = CreateSut().Run(
            [
                C(0, 100m, 101m, 99m, 100m),
                C(1, 70m, 71m, 69m, 70m),
                C(2, 70m, 71m, 69m, 70m)
            ],
            [
                S(0, TradeSide.Long),
                S(1, TradeSide.Long)
            ],
            CreateOptions() with
            {
                ProfitDistance = 10m,
                PriceDistance = 20m,
                OrderSideLimit = 2,
                CooldownSeconds = 0,
                ForceCloseAtEnd = false
            });

        Assert.Equal(2, result.Decisions.Count(x => x.Decision == "Open"));
        Assert.Equal(2, result.Executions.Count(x => x.Type == "ENTRY"));
    }

    [Fact]
    public void Run_LongInsideGap_IsBlocked()
    {
        var result = CreateSut().Run(
            [
                C(0, 100m, 101m, 99m, 100m),
                C(1, 71m, 72m, 70m, 71m),
                C(2, 71m, 72m, 70m, 71m)
            ],
            [
                S(0, TradeSide.Long),
                S(1, TradeSide.Long)
            ],
            CreateOptions() with
            {
                ProfitDistance = 10m,
                PriceDistance = 20m,
                OrderSideLimit = 2,
                CooldownSeconds = 0,
                ForceCloseAtEnd = false
            });

        Assert.Contains(result.Decisions, x =>
            x.Decision == "Block" &&
            x.Reason.Contains("GAP fail LONG"));
    }

    [Fact]
    public void Run_ShortGapExactBoundary_IsAllowed()
    {
        var result = CreateSut().Run(
            [
                C(0, 100m, 101m, 99m, 100m),
                C(1, 130m, 131m, 129m, 130m),
                C(2, 130m, 131m, 129m, 130m)
            ],
            [
                S(0, TradeSide.Short),
                S(1, TradeSide.Short)
            ],
            CreateOptions() with
            {
                ProfitDistance = 10m,
                PriceDistance = 20m,
                OrderSideLimit = 2,
                CooldownSeconds = 0,
                ForceCloseAtEnd = false
            });

        Assert.Equal(2, result.Decisions.Count(x => x.Decision == "Open"));
        Assert.Equal(2, result.Executions.Count(x => x.Type == "ENTRY"));
    }

    [Fact]
    public void Run_ShortInsideGap_IsBlocked()
    {
        var result = CreateSut().Run(
            [
                C(0, 100m, 101m, 99m, 100m),
                C(1, 129m, 130m, 128m, 129m),
                C(2, 129m, 130m, 128m, 129m)
            ],
            [
                S(0, TradeSide.Short),
                S(1, TradeSide.Short)
            ],
            CreateOptions() with
            {
                ProfitDistance = 10m,
                PriceDistance = 20m,
                OrderSideLimit = 2,
                CooldownSeconds = 0,
                ForceCloseAtEnd = false
            });

        Assert.Contains(result.Decisions, x =>
            x.Decision == "Block" &&
            x.Reason.Contains("GAP fail SHORT"));
    }

    [Fact]
    public void Run_OrderSideLimit_BlocksSecondSameSideSignal()
    {
        var result = CreateSut().Run(
            Candles(3, 100m),
            [
                S(0, TradeSide.Long),
                S(1, TradeSide.Long)
            ],
            CreateOptions() with
            {
                OrderSideLimit = 1,
                ProfitDistance = 1000m,
                PriceDistance = 1m,
                CooldownSeconds = 0,
                ForceCloseAtEnd = false
            });

        Assert.Contains(result.Decisions, x =>
            x.Decision == "Block" &&
            x.Reason.Contains("ORDER_SIDE_LIMIT"));
    }

    [Fact]
    public void Run_Cooldown_BlocksSecondSameSideSignal()
    {
        var result = CreateSut().Run(
            Candles(4, 100m),
            [
                S(0, TradeSide.Long),
                S(1, TradeSide.Long)
            ],
            CreateOptions() with
            {
                OrderSideLimit = 5,
                ProfitDistance = 1000m,
                PriceDistance = 1m,
                CooldownSeconds = 120,
                ForceCloseAtEnd = false
            });

        Assert.Contains(result.Decisions, x => x.Reason == "SIGNAL_COOLDOWN");
    }

    [Fact]
    public void Run_DisabledShort_IsBlocked()
    {
        var result = CreateSut().Run(
            Candles(2, 100m),
            [S(0, TradeSide.Short)],
            CreateOptions() with { EnableShort = false });

        Assert.Contains(result.Decisions, x => x.Reason == "SIDE_DISABLED");
        Assert.DoesNotContain(result.Executions, x => x.Type == "ENTRY");
    }

    [Fact]
    public void Run_InsufficientFreeMargin_IsBlocked()
    {
        var result = CreateSut().Run(
            Candles(2, 100m),
            [S(0, TradeSide.Long)],
            CreateOptions() with
            {
                InitialBalance = 1m,
                Quantity = 1m,
                Leverage = 1,
                EntryFeeRate = 0m
            });

        Assert.Contains(result.Decisions, x => x.Reason == "INSUFFICIENT_FREE_MARGIN");
        Assert.DoesNotContain(result.Executions, x => x.Type == "ENTRY");
    }

    [Fact]
    public void Run_ForceCloseAtEnd_ClosesRemainingPosition()
    {
        var result = CreateSut().Run(
            Candles(2, 100m),
            [S(0, TradeSide.Long)],
            CreateOptions() with
            {
                ProfitDistance = 1000m,
                ForceCloseAtEnd = true
            });

        Assert.Equal("BACKTEST_END", Assert.Single(result.Positions).ExitReason);
    }

    [Fact]
    public void Run_EntryAndExitFees_AreIncludedInNetPnl()
    {
        var result = CreateSut().Run(
            [
                C(0, 100m, 100m, 100m, 100m),
                C(1, 100m, 110m, 99m, 110m)
            ],
            [S(0, TradeSide.Long)],
            CreateOptions() with
            {
                Quantity = 1m,
                ProfitDistance = 10m,
                EntryFeeRate = 0.01m,
                ExitFeeRate = 0.01m
            });

        var position = Assert.Single(result.Positions);

        Assert.Equal(10m, position.GrossPnl);
        Assert.Equal(2.1m, position.Fees);
        Assert.Equal(7.9m, position.NetPnl);
    }

    [Fact]
    public void Run_UnsortedCandlesAndSignals_AreNormalized()
    {
        var result = CreateSut().Run(
            [
                C(1, 100m, 101m, 99m, 100m),
                C(0, 100m, 101m, 99m, 100m)
            ],
            [
                S(1, TradeSide.Short),
                S(0, TradeSide.Long)
            ],
            CreateOptions() with
            {
                ProfitDistance = 1000m,
                ForceCloseAtEnd = false
            });

        Assert.Equal(Utc(0), result.Candles[0].OpenTimeUtc);
        Assert.Equal(Utc(0), result.Signals[0].TimeUtc);
    }

    [Fact]
    public void Run_ResultKeepsBot8012Options()
    {
        var options = CreateOptions() with
        {
            PriceDistance = 321m,
            ProfitDistance = 123m,
            CooldownSeconds = 17
        };

        var result = CreateSut().Run(Candles(2, 100m), [], options);

        Assert.Same(options, result.Options);
        Assert.Equal("BOT8012", result.BotName);
        Assert.StartsWith("BOT8012_BTCUSDC_", result.RunId);
    }

    private static Bot8012BacktestEngine CreateSut()
        => new(new TpOnlyGridBacktestEngine(new GridSpacingPolicy()));

    private static Bot8012BacktestOptions CreateOptions()
        => new()
        {
            BotName = "BOT8012",
            StrategyVersion = "1.0.0",
            Symbol = "BTCUSDC",
            InitialBalance = 10_000m,
            Quantity = 1m,
            Leverage = 50,
            PriceDistance = 20m,
            ProfitDistance = 10m,
            OrderSideLimit = 2,
            CooldownSeconds = 0,
            EnableLong = true,
            EnableShort = true,
            EntryFeeRate = 0m,
            ExitFeeRate = 0m,
            SlippageBasisPoints = 0m,
            TickSize = 0.1m,
            ForceCloseAtEnd = true
        };

    private static HistoricalBotSignal S(int minute, TradeSide side)
        => new(
            Utc(minute),
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
