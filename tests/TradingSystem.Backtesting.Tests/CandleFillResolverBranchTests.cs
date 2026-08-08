using TradingSystem.Backtesting.Execution;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Models.Enums;
using TradingSystem.Domain.MarketData;
using Xunit;

namespace TradingSystem.Backtesting.Tests;

public sealed class CandleFillResolverBranchTests
{
    private readonly CandleFillResolver _sut = new();

    [Fact]
    public void Resolve_WhenNoProtectivePriceIsTouched_ReturnsNull()
    {
        var result = _sut.Resolve(
            Position(TradeSide.Long),
            Candle(96m, 104m),
            IntrabarConflictPolicy.WorstCase);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_Long_WhenOnlyStopIsTouched_ReturnsStopLoss()
    {
        var result = _sut.Resolve(
            Position(TradeSide.Long),
            Candle(94m, 104m),
            IntrabarConflictPolicy.WorstCase);

        Assert.NotNull(result);
        Assert.Equal(ExitReason.StopLoss, result.Reason);
        Assert.Equal(95m, result.Price);
    }

    [Fact]
    public void Resolve_Long_WhenOnlyTakeProfitIsTouched_ReturnsTakeProfit()
    {
        var result = _sut.Resolve(
            Position(TradeSide.Long),
            Candle(96m, 106m),
            IntrabarConflictPolicy.WorstCase);

        Assert.NotNull(result);
        Assert.Equal(ExitReason.TakeProfit, result.Reason);
        Assert.Equal(105m, result.Price);
    }

    [Fact]
    public void Resolve_Short_WhenOnlyStopIsTouched_ReturnsStopLoss()
    {
        var result = _sut.Resolve(
            Position(TradeSide.Short),
            Candle(91m, 106m),
            IntrabarConflictPolicy.WorstCase);

        Assert.NotNull(result);
        Assert.Equal(ExitReason.StopLoss, result.Reason);
        Assert.Equal(105m, result.Price);
    }

    [Fact]
    public void Resolve_WhenBothTouched_StopLossFirst_ReturnsStopLoss()
    {
        var result = _sut.Resolve(
            Position(TradeSide.Long),
            Candle(90m, 110m),
            IntrabarConflictPolicy.StopLossFirst);

        Assert.NotNull(result);
        Assert.Equal(ExitReason.StopLoss, result.Reason);
        Assert.Equal(95m, result.Price);
    }

    [Fact]
    public void Resolve_WhenBothTouched_TakeProfitFirst_ReturnsTakeProfit()
    {
        var result = _sut.Resolve(
            Position(TradeSide.Long),
            Candle(90m, 110m),
            IntrabarConflictPolicy.TakeProfitFirst);

        Assert.NotNull(result);
        Assert.Equal(ExitReason.TakeProfit, result.Reason);
        Assert.Equal(105m, result.Price);
    }

    [Fact]
    public void Resolve_WhenBothTouched_BestCase_ReturnsTakeProfit()
    {
        var result = _sut.Resolve(
            Position(TradeSide.Short),
            Candle(85m, 110m),
            IntrabarConflictPolicy.BestCase);

        Assert.NotNull(result);
        Assert.Equal(ExitReason.TakeProfit, result.Reason);
        Assert.Equal(90m, result.Price);
    }

    [Fact]
    public void Resolve_WhenBothTouched_WithUnknownPolicy_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.Resolve(
                Position(TradeSide.Long),
                Candle(90m, 110m),
                (IntrabarConflictPolicy)999));
    }

    private static BacktestPosition Position(TradeSide side) => new()
    {
        Id = 1,
        Symbol = "BTCUSDC",
        Side = side,
        EntryPrice = 100m,
        Quantity = 1m,
        EntryTimeUtc = DateTime.UtcNow,
        EntryFee = 0m,
        StopLoss = side == TradeSide.Long ? 95m : 105m,
        TakeProfit = side == TradeSide.Long ? 105m : 90m,
        InitialRiskAmount = 5m
    };

    private static MarketCandle Candle(decimal low, decimal high) =>
        new(
            "BTCUSDC",
            "1m",
            DateTime.UtcNow,
            DateTime.UtcNow,
            100m,
            high,
            low,
            100m,
            1m,
            true);
}
