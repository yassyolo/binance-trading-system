using TradingSystem.Domain.MarketData;
using TradingSystem.Jobs.Worker.Configuration;
using TradingSystem.Jobs.Worker.Workers;
using Xunit;

namespace TradingSystem.JobOrchestration.Tests;

public sealed class JobsWorkerTests
{
    [Fact]
    public void JobWorkerOptions_DefaultOptions_Succeed()
    {
        var result = new JobWorkerOptionsValidator().Validate(null, new JobWorkerOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0, 2, 30, 3, "1m")]
    [InlineData(2, 0, 30, 3, "1m")]
    [InlineData(2, 2, 0, 3, "1m")]
    [InlineData(2, 2, 30, 0, "1m")]
    [InlineData(2, 2, 30, 3, "")]
    [InlineData(2, 2, 30, 3, " ")]
    public void JobWorkerOptions_InvalidCoreValues_Fail(
        int pollSeconds,
        int batchSize,
        int timeoutMinutes,
        int maximumAttempts,
        string interval)
    {
        var options = new JobWorkerOptions
        {
            PollSeconds = pollSeconds,
            BatchSize = batchSize,
            ProcessingTimeoutMinutes = timeoutMinutes,
            MaximumAttempts = maximumAttempts,
            Interval = interval
        };

        var result = new JobWorkerOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void HistoricalDataIngestionOptions_DefaultOptions_Succeed()
    {
        var result = new HistoricalDataIngestionOptionsValidator()
            .Validate(null, new HistoricalDataIngestionOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void HistoricalDataIngestionOptions_EmptyEnvironment_Fails()
    {
        var options = new HistoricalDataIngestionOptions { Environment = "" };

        var result = new HistoricalDataIngestionOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void HistoricalDataIngestionOptions_EmptyOrWhitespaceSymbols_Fail()
    {
        var validator = new HistoricalDataIngestionOptionsValidator();

        Assert.False(validator.Validate(null, new HistoricalDataIngestionOptions { Symbols = [] }).Succeeded);
        Assert.False(validator.Validate(null, new HistoricalDataIngestionOptions { Symbols = ["BTCUSDC", " "] }).Succeeded);
    }

    [Fact]
    public void HistoricalDataIngestionOptions_EmptyOrWhitespaceIntervals_Fail()
    {
        var validator = new HistoricalDataIngestionOptionsValidator();

        Assert.False(validator.Validate(null, new HistoricalDataIngestionOptions { Intervals = [] }).Succeeded);
        Assert.False(validator.Validate(null, new HistoricalDataIngestionOptions { Intervals = ["1m", " "] }).Succeeded);
    }

    [Theory]
    [InlineData(0, 30, 3)]
    [InlineData(-1, 30, 3)]
    [InlineData(5, 0, 3)]
    [InlineData(5, -1, 3)]
    [InlineData(5, 30, -1)]
    public void HistoricalDataIngestionOptions_InvalidNumericValues_Fail(
        int pollMinutes,
        int lookbackDays,
        int overlapCandles)
    {
        var options = new HistoricalDataIngestionOptions
        {
            PollMinutes = pollMinutes,
            InitialLookbackDays = lookbackDays,
            OverlapCandles = overlapCandles
        };

        var result = new HistoricalDataIngestionOptionsValidator().Validate(null, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void HistoricalDataIngestionOptions_ZeroOverlap_IsAcceptedByValidator()
    {
        var options = new HistoricalDataIngestionOptions { OverlapCandles = 0 };

        var result = new HistoricalDataIngestionOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("15m", 15)]
    [InlineData("2h", 120)]
    [InlineData("3d", 4320)]
    public void ParseInterval_ValidValues_ReturnExpectedMinutes(string value, int minutes)
    {
        var result = HistoricalDataIngestionWorker.ParseInterval(value);

        Assert.Equal(TimeSpan.FromMinutes(minutes), result);
    }

    [Theory]
    [InlineData("1M")]
    [InlineData("1H")]
    [InlineData("1D")]
    [InlineData("1s")]
    [InlineData("abc")]
    [InlineData("1")]
    public void ParseInterval_UnsupportedFormats_Throw(string value)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => HistoricalDataIngestionWorker.ParseInterval(value));
    }

    [Fact]
    public void DetectGaps_DuplicateTimestamp_DoesNotCreateFalseGap()
    {
        var candles = new[]
        {
            Candle(0),
            Candle(1),
            Candle(1),
            Candle(2)
        };

        var result = HistoricalDataIngestionWorker.DetectGaps(
            "BTCUSDC",
            "1m",
            candles,
            TimeSpan.FromMinutes(1));

        Assert.Empty(result);
    }

    [Fact]
    public void DetectGaps_OverlappingTimestamp_DoesNotCreateFalseGap()
    {
        var first = Candle(0);
        var overlapping = first with
        {
            OpenTimeUtc = first.OpenTimeUtc.AddSeconds(30),
            CloseTimeUtc = first.CloseTimeUtc.AddSeconds(30)
        };

        var result = HistoricalDataIngestionWorker.DetectGaps(
            "BTCUSDC",
            "1m",
            [first, overlapping, Candle(1)],
            TimeSpan.FromMinutes(1));

        Assert.Empty(result);
    }

    [Fact]
    public void DetectGaps_ExactTwoCandleHole_ReportsCorrectInclusiveRange()
    {
        var candles = new[]
        {
            Candle(0),
            Candle(3)
        };

        var result = HistoricalDataIngestionWorker.DetectGaps(
            "BTCUSDC",
            "1m",
            candles,
            TimeSpan.FromMinutes(1));

        var gap = Assert.Single(result);
        Assert.Equal(2, gap.MissingCandles);
        Assert.Equal(Candle(1).OpenTimeUtc, gap.FromUtc);
        Assert.Equal(Candle(2).OpenTimeUtc, gap.ToUtc);
    }

    [Fact]
    public void DetectGaps_DoesNotInferMissingCandlesBeforeFirstOrAfterLastRow()
    {
        var candles = new[]
        {
            Candle(10),
            Candle(11)
        };

        var result = HistoricalDataIngestionWorker.DetectGaps(
            "BTCUSDC",
            "1m",
            candles,
            TimeSpan.FromMinutes(1));

        Assert.Empty(result);
    }

    [Fact]
    public void DetectGaps_NonPositiveDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => HistoricalDataIngestionWorker.DetectGaps(
                "BTCUSDC",
                "1m",
                [Candle(0), Candle(1)],
                TimeSpan.Zero));
    }

    private static MarketCandle Candle(int minute)
    {
        var open = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMinutes(minute);

        return new MarketCandle(
            "BTCUSDC",
            "1m",
            open,
            open.AddMinutes(1).AddMilliseconds(-1),
            1m,
            1m,
            1m,
            1m,
            1m,
            true);
    }
}
