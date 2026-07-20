using TradingSystem.Backtesting.Execution;
using TradingSystem.Backtesting.Models;
using TradingSystem.Domain.MarketData;
using Xunit;

namespace TradingSystem.Backtesting.Tests;

public sealed class CandleFillResolverTests
{
    private readonly CandleFillResolver _sut = new();

    [Fact]
    public void Resolve_Long_WhenStopAndTakeProfitAreTouched_WorstCaseReturnsStop()
    {
        var fill = _sut.Resolve(Position(TradeSide.Long), Candle(90m, 110m), IntrabarConflictPolicy.WorstCase);
        Assert.NotNull(fill);
        Assert.Equal(ExitReason.StopLoss, fill.Reason);
        Assert.Equal(95m, fill.Price);
    }

    [Fact]
    public void Resolve_Short_WhenOnlyTakeProfitIsTouched_ReturnsTakeProfit()
    {
        var fill = _sut.Resolve(Position(TradeSide.Short), Candle(89m, 101m), IntrabarConflictPolicy.WorstCase);
        Assert.NotNull(fill);
        Assert.Equal(ExitReason.TakeProfit, fill.Reason);
        Assert.Equal(90m, fill.Price);
    }

    private static BacktestPosition Position(TradeSide side) => new()
    {
        Id = 1, Symbol = "BTCUSDC", Side = side, EntryPrice = 100m, Quantity = 1m,
        EntryTimeUtc = DateTime.UtcNow, EntryFee = 0m,
        StopLoss = side == TradeSide.Long ? 95m : 105m,
        TakeProfit = side == TradeSide.Long ? 105m : 90m,
        InitialRiskAmount = 5m
    };

    private static MarketCandle Candle(decimal low, decimal high)
        => new("BTCUSDC", "1m", DateTime.UtcNow, DateTime.UtcNow,
            100m, high, low, 100m, 1m, true);
}
