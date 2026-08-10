using TradingSystem.Binance.Execution.Models;
using TradingSystem.Domain.Enums;
using Xunit;

namespace TradingSystem.Binance.Tests;

public sealed class BinanceOrderSideTests
{
    [Theory]
    [InlineData(PositionSide.Long, "BUY")]
    [InlineData(PositionSide.Short, "SELL")]
    public void Entry_ReturnsExpectedSide(PositionSide side, string expected)
    {
        Assert.Equal(expected, BinanceOrderSide.Entry(side));
    }

    [Theory]
    [InlineData(PositionSide.Long, "SELL")]
    [InlineData(PositionSide.Short, "BUY")]
    public void Close_ReturnsExpectedSide(PositionSide side, string expected)
    {
        Assert.Equal(expected, BinanceOrderSide.Close(side));
    }

    [Theory]
    [InlineData(PositionSide.Long, "LONG")]
    [InlineData(PositionSide.Short, "SHORT")]
    public void Position_ReturnsExpectedValue(PositionSide side, string expected)
    {
        Assert.Equal(expected, BinanceOrderSide.Position(side));
    }

    [Theory]
    [InlineData("LONG", PositionSide.Long)]
    [InlineData("long", PositionSide.Long)]
    [InlineData("SHORT", PositionSide.Short)]
    [InlineData("short", PositionSide.Short)]
    public void TryParsePosition_ValidValue_ReturnsTrue(string value, PositionSide expected)
    {
        Assert.True(BinanceOrderSide.TryParsePosition(value, out var side));
        Assert.Equal(expected, side);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("BUY")]
    public void TryParsePosition_InvalidValue_ReturnsFalse(string? value)
    {
        Assert.False(BinanceOrderSide.TryParsePosition(value, out _));
    }
}
