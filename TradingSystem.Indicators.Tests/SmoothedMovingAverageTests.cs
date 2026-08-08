using TradingSystem.Indicators.Common;
using Xunit;

namespace TradingSystem.Indicators.Tests;

public sealed class SmoothedMovingAverageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveLength_Throws(int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SmoothedMovingAverage(length));
    }

    [Fact]
    public void Update_DuringWarmup_ReturnsRunningAverage()
    {
        var sut = new SmoothedMovingAverage(3);

        Assert.Equal(3m, sut.Update(3m));
        Assert.Equal(4m, sut.Update(5m));
        Assert.Equal(5m, sut.Update(7m));
    }

    [Fact]
    public void Update_AfterWarmup_AppliesSmoothedFormula()
    {
        var sut = new SmoothedMovingAverage(3);
        sut.Update(3m);
        sut.Update(6m);
        sut.Update(9m);

        Assert.Equal(8m, sut.Update(12m));
    }
}
