using TradingSystem.Binance.Resilience.Configuration;
using Xunit;

namespace TradingSystem.Binance.Tests;

public sealed class BinanceRetryOptionsValidatorTests
{
    private readonly BinanceRetryOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        Assert.True(_sut.Validate(null, new BinanceRetryOptions()).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Validate_MaximumAttemptsOutsideRange_Fails(int value)
    {
        Assert.False(_sut.Validate(null, new BinanceRetryOptions { MaximumAttempts = value }).Succeeded);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(60001)]
    public void Validate_InitialDelayOutsideRange_Fails(int value)
    {
        Assert.False(_sut.Validate(null, new BinanceRetryOptions { InitialDelayMilliseconds = value }).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(120001)]
    public void Validate_MaximumDelayOutsideRange_Fails(int value)
    {
        Assert.False(_sut.Validate(null, new BinanceRetryOptions { MaximumDelayMilliseconds = value }).Succeeded);
    }

    [Fact]
    public void Validate_MaximumDelayBelowInitialDelay_Fails()
    {
        var options = new BinanceRetryOptions { InitialDelayMilliseconds = 1000, MaximumDelayMilliseconds = 999 };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(11)]
    public void Validate_BackoffMultiplierOutsideRange_Fails(double value)
    {
        Assert.False(_sut.Validate(null, new BinanceRetryOptions { BackoffMultiplier = value }).Succeeded);
    }
}
