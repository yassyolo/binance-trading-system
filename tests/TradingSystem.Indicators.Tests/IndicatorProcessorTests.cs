using Microsoft.Extensions.Options;
using TradingSystem.Domain.MarketData;
using TradingSystem.Indicators.Alligator;
using TradingSystem.Indicators.Alligator.Configuration;
using TradingSystem.Indicators.Bollinger;
using TradingSystem.Indicators.Bollinger.Configuration;
using Xunit;

namespace TradingSystem.Indicators.Tests;

public sealed class IndicatorProcessorTests
{
    [Fact]
    public void Alligator_ProcessBeforeInitialize_ReturnsNull()
    {
        var sut = new AlligatorIndicatorProcessor(Options.Create(Alligator()));
        Assert.Null(sut.Process(Candle(0, 100m), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Alligator_OpenCandle_ReturnsNull()
    {
        var sut = new AlligatorIndicatorProcessor(Options.Create(Alligator()));
        sut.Initialize("BTCUSDC", "5m", [Candle(0, 100m)]);

        Assert.Null(sut.Process(Candle(1, 101m, false), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Alligator_AfterRequiredHistory_ProducesSnapshot()
    {
        var options = Alligator();
        var sut = new AlligatorIndicatorProcessor(Options.Create(options));
        sut.Initialize("BTCUSDC", "5m", [Candle(0, 100m), Candle(1, 101m)]);

        var result = sut.Process(Candle(2, 102m), DateTimeOffset.UtcNow);

        Assert.NotNull(result);
        Assert.Equal("alligator_ma", result!.Type);
        Assert.Contains("alligator_jaw", result.Indicators.Keys);
        Assert.Contains("sma200", result.Indicators.Keys);
    }

    [Fact]
    public void Alligator_DuplicateCloseTime_ReturnsNull()
    {
        var sut = new AlligatorIndicatorProcessor(Options.Create(Alligator()));
        var candle = Candle(0, 100m);
        sut.Initialize("BTCUSDC", "5m", [candle]);

        Assert.Null(sut.Process(candle, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Bollinger_ProcessBeforeInitialize_ReturnsNull()
    {
        var sut = new BollingerIndicatorProcessor(Options.Create(Bollinger()));
        Assert.Null(sut.Process(Candle(0, 100m, interval: "1m"), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Bollinger_AfterWarmup_ProducesBasisUpperLower()
    {
        var sut = new BollingerIndicatorProcessor(Options.Create(Bollinger()));
        sut.Initialize("BTCUSDC", "1m", [Candle(0, 100m, interval: "1m")]);

        var result = sut.Process(Candle(1, 102m, interval: "1m"), DateTimeOffset.UtcNow);

        Assert.NotNull(result);
        Assert.Contains("bb.basis", result!.Indicators.Keys);
        Assert.Contains("bb.upper", result.Indicators.Keys);
        Assert.Contains("bb.lower", result.Indicators.Keys);
    }

    [Fact]
    public void Bollinger_SecondSnapshotCarriesPreviousValues()
    {
        var sut = new BollingerIndicatorProcessor(Options.Create(Bollinger()));
        sut.Initialize("BTCUSDC", "1m", [Candle(0, 100m, interval: "1m")]);
        var first = sut.Process(Candle(1, 102m, interval: "1m"), DateTimeOffset.UtcNow);
        var second = sut.Process(Candle(2, 104m, interval: "1m"), DateTimeOffset.UtcNow);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Indicators["bb.basis"].Value, second!.Indicators["bb.basis"].PreviousValue);
    }

    private static AlligatorOptions Alligator() => new() { Symbols = ["BTCUSDC"], Intervals = ["5m"], HistoryLimit = 10, SmaLength = 2, JawLength = 2, TeethLength = 2, LipsLength = 2 };
    private static BollingerOptions Bollinger() => new() { Symbols = ["BTCUSDC"], Intervals = ["1m"], HistoryLimit = 10, Bands = [new BollingerBandOptions { Name = "bb", Length = 2, Source = "close", Multiplier = 2m }] };

    private static MarketCandle Candle(int minute, decimal close, bool closed = true, string interval = "5m")
    {
        var open = new DateTime(2026, 1, 1, 0, minute, 0, DateTimeKind.Utc);
        return new MarketCandle("BTCUSDC", interval, open, open.AddMinutes(1), close, close + 1, close - 1, close, 1m, closed);
    }
}
