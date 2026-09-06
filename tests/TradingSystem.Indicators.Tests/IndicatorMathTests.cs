using TradingSystem.Indicators.Bollinger;
using Xunit;

namespace TradingSystem.Indicators.Tests;

public sealed class IndicatorMathTests
{
    [Fact]
    public void Average_ReturnsArithmeticMean()
    {
        Assert.Equal(3m, IndicatorMath.Average([1m, 3m, 5m]));
    }

    [Fact]
    public void StandardDeviation_ConstantValues_ReturnsZero()
    {
        Assert.Equal(0m, IndicatorMath.StandardDeviation([5m, 5m, 5m]));
    }

    [Fact]
    public void StandardDeviation_KnownPopulation_ReturnsExpectedValue()
    {
        var result = IndicatorMath.StandardDeviation([2m, 4m, 4m, 4m, 5m, 5m, 7m, 9m]);
        Assert.Equal(2m, result);
    }
}
