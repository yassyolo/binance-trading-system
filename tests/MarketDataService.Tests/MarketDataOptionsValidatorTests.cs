using MarketDataService.Configuration;
using Xunit;

namespace MarketDataService.Tests;

public sealed class MarketDataOptionsValidatorTests
{
    private readonly MarketDataOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = _sut.Validate(null, new MarketDataOptions());
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptySymbols_Fails()
    {
        var result = _sut.Validate(null, Options(x => x.Symbols = []));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyIntervals_Fails()
    {
        var result = _sut.Validate(null, Options(x => x.Intervals = []));
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://fstream.binance.com")]
    [InlineData("not-a-url")]
    public void Validate_InvalidWebSocketUrl_Fails(string value)
    {
        var options = Valid();
        options.BinanceWebSocketBaseUrl = value;

        var result = _sut.Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_WsUrl_Succeeds()
    {
        var options = Valid();
        options.BinanceWebSocketBaseUrl = "ws://localhost:8080/stream";

        Assert.True(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveReconnectDelay_Fails(int value)
    {
        var options = Valid();
        options.ReconnectDelaySeconds = value;

        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_MaximumReconnectBelowReconnectDelay_Fails()
    {
        var options = Valid();
        options.ReconnectDelaySeconds = 10;
        options.MaximumReconnectDelaySeconds = 9;

        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveKeepAlive_Fails(int value)
    {
        var options = Valid();
        options.KeepAliveIntervalSeconds = value;

        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1023)]
    public void Validate_ReceiveBufferBelow1024_Fails(int value)
    {
        var options = Valid();
        options.ReceiveBufferSizeBytes = value;

        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NonPositiveLatestKlineTtl_Fails()
    {
        var options = Valid();
        options.LatestKlineTtlSeconds = 0;

        Assert.False(_sut.Validate(null, options).Succeeded);
    }


    private static MarketDataOptions Valid() => new();

    private static MarketDataOptions Options(Action<MarketDataOptions> configure)
    {
        var options = Valid();
        configure(options);
        return options;
    }
}
