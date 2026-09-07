using TradingSystem.Domain.Enums;
using TradingSystem.PaperTrading.Models;
using TradingSystem.PaperTrading.Models.Enums;
using TradingSystem.PaperTrading.Workers;
using Xunit;

namespace TradingSystem.PaperTrading.Tests;

public sealed class PaperFillWorkerTests
{
    [Theory]
    [InlineData(PositionSide.Long, 101, "PAPER_TAKE_PROFIT")]
    [InlineData(PositionSide.Long, 98, "PAPER_STOP_LOSS")]
    [InlineData(PositionSide.Short, 99, "PAPER_TAKE_PROFIT")]
    [InlineData(PositionSide.Short, 102, "PAPER_STOP_LOSS")]
    public void ResolveCloseReason_ClosesAtConfiguredBoundary(PositionSide side, decimal price, string expected)
    {
        var position = Position(side);

        var result = PaperFillWorker.ResolveCloseReason(
            position,
            price);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveCloseReason_Long_InsideBoundaries_ReturnsNull()
    {
        var position = Position(PositionSide.Long);

        var result = PaperFillWorker.ResolveCloseReason(
            position,
            100m);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveCloseReason_Short_InsideBoundaries_ReturnsNull()
    {
        var position = Position(PositionSide.Short);

        var result = PaperFillWorker.ResolveCloseReason(
            position,
            100m);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveCloseReason_Long_AtTakeProfitBoundary_ReturnsTakeProfit()
    {
        var position = Position(PositionSide.Long);

        var result = PaperFillWorker.ResolveCloseReason(
            position,
            101m);

        Assert.Equal("PAPER_TAKE_PROFIT", result);
    }

    [Fact]
    public void ResolveCloseReason_Long_AtStopLossBoundary_ReturnsStopLoss()
    {
        var position = Position(PositionSide.Long);

        var result = PaperFillWorker.ResolveCloseReason(
            position,
            98m);

        Assert.Equal("PAPER_STOP_LOSS", result);
    }

    [Fact]
    public void ResolveCloseReason_Short_AtTakeProfitBoundary_ReturnsTakeProfit()
    {
        var position = Position(PositionSide.Short);

        var result = PaperFillWorker.ResolveCloseReason(
            position,
            99m);

        Assert.Equal("PAPER_TAKE_PROFIT", result);
    }

    [Fact]
    public void ResolveCloseReason_Short_AtStopLossBoundary_ReturnsStopLoss()
    {
        var position = Position(PositionSide.Short);

        var result = PaperFillWorker.ResolveCloseReason(
            position,
            102m);

        Assert.Equal("PAPER_STOP_LOSS", result);
    }

    private static PaperTradingPosition Position(
        PositionSide side)
    {
        return new PaperTradingPosition
        {
            PositionId = Guid.NewGuid(),
            ShortId = "P1",
            SignalId = "signal-1",
            StrategyVersion = "1.0.0",
            BotName = "BOT8012",
            Symbol = "BTCUSDC",
            Side = side,
            Quantity = 1m,
            EntryPrice = 100m,
            TakeProfitPrice = side == PositionSide.Long ? 101m : 99m,
            StopLossPrice = side == PositionSide.Long ? 98m : 102m,
            EntryFee = 0m,
            ExitPrice = null,
            ExitFee = null,
            RealizedPnl = null,
            Status = PaperPositionStatus.Open,
            Source = "test",
            OpenedAtUtc = DateTime.UtcNow,
            ClosedAtUtc = null,
            CloseReason = null,
            Version = 1
        };
    }
}