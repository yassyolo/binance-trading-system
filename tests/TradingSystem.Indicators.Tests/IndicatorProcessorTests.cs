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

    [Fact]
    public void Alligator_UsesMedianPriceForLines_AndCloseForSma()
    {
        var sut = new AlligatorIndicatorProcessor(Options.Create(Alligator()));
        sut.Initialize("BTCUSDC", "5m", [Candle(0, open: 100m, high: 110m, low: 90m, close: 105m)]);

        var result = sut.Process(Candle(5, open: 120m, high: 130m, low: 110m, close: 125m), PublishedAt);

        Assert.NotNull(result);
        Assert.Equal(110m, result!.Indicators["alligator_jaw"].Value);
        Assert.Equal(110m, result.Indicators["alligator_teeth"].Value);
        Assert.Equal(110m, result.Indicators["alligator_lips"].Value);
        Assert.Equal(115m, result.Indicators["sma200"].Value);
    }

    [Fact]
    public void Alligator_Initialize_IgnoresOpenCandles_AndSortsClosedHistory()
    {
        var sut = new AlligatorIndicatorProcessor(Options.Create(Alligator()));
        sut.Initialize("BTCUSDC", "5m",
        [
            Candle(5, 102m, 103m, 101m, 102m),
            Candle(10, 1_000m, 1_001m, 999m, 1_000m, closed: false),
            Candle(0, 100m, 101m, 99m, 100m)
        ]);

        var result = sut.Process(Candle(10, 104m, 105m, 103m, 104m), PublishedAt);

        Assert.NotNull(result);
        Assert.Equal(103m, result!.Indicators["sma200"].Value);
    }

    [Fact]
    public void Alligator_StateKeyMatching_IsCaseInsensitive()
    {
        var sut = new AlligatorIndicatorProcessor(Options.Create(Alligator()));
        sut.Initialize("btcusdc", "5M", [Candle(0, 100m, 101m, 99m, 100m, symbol: "BTCUSDC", interval: "5m")]);

        var result = sut.Process(Candle(5, 102m, 103m, 101m, 102m, symbol: "BtCuSdC", interval: "5m"), PublishedAt);

        Assert.NotNull(result);
    }

    [Fact]
    public void Alligator_SnapshotCarriesInputAndPublicationTimestamps()
    {
        var sut = new AlligatorIndicatorProcessor(Options.Create(Alligator()));
        sut.Initialize("BTCUSDC", "5m", [Candle(0, 100m, 101m, 99m, 100m)]);
        var candle = Candle(5, 102m, 103m, 101m, 102m);

        var result = sut.Process(candle, PublishedAt);

        Assert.NotNull(result);
        Assert.Equal(new DateTimeOffset(candle.OpenTimeUtc).ToUnixTimeMilliseconds(), result!.CandleOpenTime);
        Assert.Equal(new DateTimeOffset(candle.CloseTimeUtc).ToUnixTimeMilliseconds(), result.CandleCloseTime);
        Assert.Equal(PublishedAt.ToUnixTimeMilliseconds(), result.PublishedAt);
        Assert.Equal("BTCUSDC", result.Symbol);
        Assert.Equal("5m", result.Timeframe);
    }

    [Fact]
    public void Alligator_RequiredHistory_UsesLargerOfHistoryLimitAndSmaLength()
    {
        var options = Alligator();
        options.HistoryLimit = 50;
        options.SmaLength = 80;

        var sut = new AlligatorIndicatorProcessor(Options.Create(options));

        Assert.Equal(80, sut.RequiredHistory);
    }

    [Fact]
    public void Bollinger_OpenSource_UsesOpenValuesInsteadOfCloseValues()
    {
        var options = Bollinger(new BollingerBandOptions { Name = "open_band", Length = 2, Source = "open", Multiplier = 2m });
        var sut = new BollingerIndicatorProcessor(Options.Create(options));
        sut.Initialize("BTCUSDC", "1m", [Candle(0, open: 90m, high: 101m, low: 89m, close: 100m, interval: "1m")]);

        var result = sut.Process(Candle(1, open: 110m, high: 111m, low: 99m, close: 100m, interval: "1m"), PublishedAt);

        Assert.NotNull(result);
        Assert.Equal(100m, result!.Indicators["open_band.basis"].Value);
        Assert.Equal(120m, result.Indicators["open_band.upper"].Value);
        Assert.Equal(80m, result.Indicators["open_band.lower"].Value);
    }

    [Fact]
    public void Bollinger_Metadata_DescribesConfiguredBand()
    {
        var options = Bollinger(new BollingerBandOptions { Name = "bb", Length = 2, Source = "close", Multiplier = 2.5m });
        var sut = new BollingerIndicatorProcessor(Options.Create(options));
        sut.Initialize("BTCUSDC", "1m", [Candle(0, 100m, 101m, 99m, 100m, interval: "1m")]);

        var result = sut.Process(Candle(1, 102m, 103m, 101m, 102m, interval: "1m"), PublishedAt);
        var metadata = result!.Indicators["bb.upper"].Metadata;

        Assert.NotNull(metadata);
        Assert.Equal("2", metadata!["length"]);
        Assert.Equal("close", metadata["source"]);
        Assert.Equal("SMA", metadata["ma_type"]);
        Assert.Equal("2.5", metadata["multiplier"]);
    }

    [Fact]
    public void Bollinger_OpenCandle_ReturnsNullWithoutChangingPreviousValues()
    {
        var sut = new BollingerIndicatorProcessor(Options.Create(Bollinger()));
        sut.Initialize("BTCUSDC", "1m", [Candle(0, 100m, 101m, 99m, 100m, interval: "1m")]);
        var first = sut.Process(Candle(1, 102m, 103m, 101m, 102m, interval: "1m"), PublishedAt);

        var ignored = sut.Process(Candle(2, 1_000m, 1_001m, 999m, 1_000m, closed: false, interval: "1m"), PublishedAt);
        var second = sut.Process(Candle(3, 104m, 105m, 103m, 104m, interval: "1m"), PublishedAt);

        Assert.Null(ignored);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Indicators["bb.basis"].Value, second!.Indicators["bb.basis"].PreviousValue);
    }

    [Fact]
    public void Bollinger_DuplicateCloseTime_ReturnsNull()
    {
        var sut = new BollingerIndicatorProcessor(Options.Create(Bollinger()));
        var first = Candle(0, 100m, 101m, 99m, 100m, interval: "1m");
        sut.Initialize("BTCUSDC", "1m", [first]);
        var candle = Candle(1, 102m, 103m, 101m, 102m, interval: "1m");
        Assert.NotNull(sut.Process(candle, PublishedAt));

        Assert.Null(sut.Process(candle, PublishedAt.AddSeconds(1)));
    }

    [Fact]
    public void Bollinger_MultipleBands_PublishIndependentKeysAndSources()
    {
        var options = Bollinger(
            new BollingerBandOptions { Name = "open2", Length = 2, Source = "open", Multiplier = 1m },
            new BollingerBandOptions { Name = "close2", Length = 2, Source = "close", Multiplier = 1m });
        var sut = new BollingerIndicatorProcessor(Options.Create(options));
        sut.Initialize("BTCUSDC", "1m", [Candle(0, open: 90m, high: 101m, low: 89m, close: 100m, interval: "1m")]);

        var result = sut.Process(Candle(1, open: 110m, high: 121m, low: 99m, close: 120m, interval: "1m"), PublishedAt);

        Assert.NotNull(result);
        Assert.Equal(100m, result!.Indicators["open2.basis"].Value);
        Assert.Equal(110m, result.Indicators["close2.basis"].Value);
        Assert.Contains("open2.upper", result.Indicators.Keys);
        Assert.Contains("close2.lower", result.Indicators.Keys);
    }

    [Fact]
    public void Bollinger_RequiredHistory_UsesLongestConfiguredBandWhenLarger()
    {
        var options = Bollinger(new BollingerBandOptions { Name = "slow", Length = 250, Source = "close", Multiplier = 2m });
        options.HistoryLimit = 200;

        var sut = new BollingerIndicatorProcessor(Options.Create(options));

        Assert.Equal(250, sut.RequiredHistory);
    }

    private static readonly DateTimeOffset PublishedAt = new(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);

    private static AlligatorOptions Alligator() => new()
    {
        Symbols = ["BTCUSDC"],
        Intervals = ["5m"],
        HistoryLimit = 10,
        SmaLength = 2,
        JawLength = 2,
        TeethLength = 2,
        LipsLength = 2
    };

    private static BollingerOptions Bollinger(params BollingerBandOptions[] bands) => new()
    {
        Symbols = ["BTCUSDC"],
        Intervals = ["1m"],
        HistoryLimit = 10,
        Bands = bands.Length == 0
            ? [new BollingerBandOptions { Name = "bb", Length = 2, Source = "close", Multiplier = 2m }]
            : [.. bands]
    };

    private static MarketCandle Candle(
        int minute,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        bool closed = true,
        string symbol = "BTCUSDC",
        string interval = "5m")
    {
        var openTime = new DateTime(2026, 1, 1, 0, minute, 0, DateTimeKind.Utc);
        return new MarketCandle(symbol, interval, openTime, openTime.AddMinutes(1), open, high, low, close, 1m, closed);
    }

    private static BollingerOptions Bollinger() => new() { Symbols = ["BTCUSDC"], Intervals = ["1m"], HistoryLimit = 10, Bands = [new BollingerBandOptions { Name = "bb", Length = 2, Source = "close", Multiplier = 2m }] };

    private static MarketCandle Candle(int minute, decimal close, bool closed = true, string interval = "5m")
    {
        var open = new DateTime(2026, 1, 1, 0, minute, 0, DateTimeKind.Utc);
        return new MarketCandle("BTCUSDC", interval, open, open.AddMinutes(1), close, close + 1, close - 1, close, 1m, closed);
    }
}
