using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Alligator;
using TradingSystem.Strategies.Alligator.Models;
using Xunit;

namespace TradingSystem.Strategies.Tests;

public sealed class AlligatorEntryPolicyBranchTests
{
    private readonly AlligatorEntryPolicy _sut = new();

    [Fact]
    public void Evaluate_OpenCandle_ReturnsNull()
    {
        var result = _sut.Evaluate(
            Bullish() with { IsClosed = false },
            Parameters());

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_WrongSymbol_ReturnsNull()
    {
        var result = _sut.Evaluate(
            Bullish() with { Symbol = "ETHUSDC" },
            Parameters());

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_WrongInterval_ReturnsNull()
    {
        var result = _sut.Evaluate(
            Bullish() with { Interval = "1h" },
            Parameters());

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_CandleRangeBelowMinimum_ReturnsNull()
    {
        var input = Bullish() with
        {
            Low = 99m,
            High = 101m
        };

        var result = _sut.Evaluate(
            input,
            Parameters() with
            {
                MinimumCandleRange = 5m
            });

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_BullishCross_WhenLongDisabled_ReturnsNull()
    {
        var result = _sut.Evaluate(
            Bullish(),
            Parameters() with
            {
                EnableLong = false
            });

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_BullishCross_BelowSma_WhenFilterEnabled_ReturnsNull()
    {
        var result = _sut.Evaluate(
            Bullish() with
            {
                Close = 120m,
                Sma200 = 130m
            },
            Parameters() with
            {
                UseMa200Filter = true
            });

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_BearishCross_ReturnsShort()
    {
        var result = _sut.Evaluate(
            Bearish(),
            Parameters());

        Assert.NotNull(result);
        Assert.Equal(PositionSide.Short, result.Side);
    }

    [Fact]
    public void Evaluate_BearishCross_WhenShortDisabled_ReturnsNull()
    {
        var result = _sut.Evaluate(
            Bearish(),
            Parameters() with
            {
                EnableShort = false
            });

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_EmptyStrategySymbol_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _sut.Evaluate(
                Bullish(),
                Parameters() with
                {
                    Symbol = ""
                }));
    }

    [Fact]
    public void Evaluate_EmptyStrategyInterval_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            _sut.Evaluate(
                Bullish(),
                Parameters() with
                {
                    Interval = ""
                }));
    }

    [Fact]
    public void Evaluate_NegativeMinimumRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Evaluate(
                Bullish(),
                Parameters() with
                {
                    MinimumCandleRange = -1m
                }));
    }

    private static AlligatorEntryInput Bullish() =>
        new(
            "BTCUSDC",
            "5m",
            true,
            100m,
            180m,
            90m,
            160m,
            120m,
            150m);

    private static AlligatorEntryInput Bearish() =>
        new(
            "BTCUSDC",
            "5m",
            true,
            160m,
            180m,
            90m,
            100m,
            120m,
            150m);

    private static AlligatorEntryParameters Parameters() =>
        new(
            "BTCUSDC",
            "5m",
            true,
            true,
            false,
            1m);
}
