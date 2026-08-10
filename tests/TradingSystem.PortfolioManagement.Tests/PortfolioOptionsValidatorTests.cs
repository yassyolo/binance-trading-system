using TradingSystem.PortfolioManagement.Configuration;
using Xunit;

namespace TradingSystem.PortfolioManagement.Tests;

public sealed class PortfolioOptionsValidatorTests
{
    private readonly PortfolioOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        Assert.True(_sut.Validate(null, new PortfolioOptions()).Succeeded);
    }

    [Fact]
    public void Validate_NonPositiveInitialEquity_Fails()
    {
        var options = new PortfolioOptions { InitialEquity = 0 };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NegativeCacheMilliseconds_Fails()
    {
        var options = new PortfolioOptions { SnapshotCacheMilliseconds = -1 };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(126)]
    public void Validate_InvalidDefaultLeverage_Fails(int value)
    {
        var options = new PortfolioOptions { DefaultLeverage = value };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Validate_InvalidRetryCount_Fails(int value)
    {
        var options = new PortfolioOptions { LoadRetryCount = value };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(30001)]
    public void Validate_InvalidRetryDelay_Fails(int value)
    {
        var options = new PortfolioOptions { LoadRetryDelayMilliseconds = value };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_EmptyBotName_Fails()
    {
        var options = new PortfolioOptions { Bots = ["BOT1", ""] };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_DuplicateBotNames_FailsCaseInsensitive()
    {
        var options = new PortfolioOptions { Bots = ["BOT1", "bot1"] };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }
}
