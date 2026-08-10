using TradingSystem.Binance.Configuration;
using Xunit;

namespace TradingSystem.Binance.Tests;

public sealed class BinanceFuturesOptionsValidatorTests
{
    private readonly BinanceFuturesOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        Assert.True(_sut.Validate(null, new BinanceFuturesOptions()).Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://binance.test")]
    [InlineData("not-a-url")]
    public void Validate_InvalidBaseUrl_Fails(string value)
    {
        var options = new BinanceFuturesOptions { BaseUrl = value };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60001)]
    public void Validate_ReceiveWindowOutsideRange_Fails(int value)
    {
        var options = new BinanceFuturesOptions { ReceiveWindow = value };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NonPositiveExchangeInfoCacheDuration_Fails()
    {
        var options = new BinanceFuturesOptions { ExchangeInfoCacheDuration = TimeSpan.Zero };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_SignedOperationsWithoutApiKey_Fails()
    {
        var options = Signed();
        options.ApiKey = "";

        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_SignedOperationsWithoutSecretKey_Fails()
    {
        var options = Signed();
        options.SecretKey = "";

        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_SignedOperationsWithCredentials_Succeeds()
    {
        Assert.True(_sut.Validate(null, Signed()).Succeeded);
    }

    private static BinanceFuturesOptions Signed() => new() { RequireSignedOperations = true, ApiKey = "key", SecretKey = "secret" };
}
