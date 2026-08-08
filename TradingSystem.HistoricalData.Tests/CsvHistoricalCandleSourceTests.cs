using TradingSystem.HistoricalData;
using Xunit;

namespace TradingSystem.HistoricalData.Tests;

public sealed class CsvHistoricalCandleSourceTests
{
    [Fact]
    public async Task LoadAsync_NormalizesSymbolAndInterval()
    {
        using var file = Csv("open_time_utc,open,high,low,close,volume\n2026-01-01T00:00:00Z,100,110,90,105,12");
        var result = await new CsvHistoricalCandleSource(file.Path).LoadAsync(" btcusdc ", " 1M ", null, null);

        var candle = Assert.Single(result);
        Assert.Equal("BTCUSDC", candle.Symbol);
        Assert.Equal("1m", candle.Interval);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_Throws()
    {
        var sut = new CsvHistoricalCandleSource(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => sut.LoadAsync("BTCUSDC", "1m", null, null));
    }

    [Theory]
    [InlineData("", "1m")]
    [InlineData("BTCUSDC", "")]
    public async Task LoadAsync_EmptySymbolOrInterval_Throws(string symbol, string interval)
    {
        using var file = Csv("open_time_utc,open,high,low,close\n");
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new CsvHistoricalCandleSource(file.Path).LoadAsync(symbol, interval, null, null));
    }

    [Fact]
    public async Task LoadAsync_EmptyFile_ReturnsEmpty()
    {
        using var file = Csv("");
        var result = await new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAsync_MissingRequiredColumns_Throws()
    {
        using var file = Csv("time,open,close\n2026-01-01T00:00:00Z,1,1");
        await Assert.ThrowsAsync<InvalidOperationException>(() => new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null));
    }

    [Fact]
    public async Task LoadAsync_InvalidRange_Throws()
    {
        using var file = Csv("open_time_utc,open,high,low,close\n");
        var now = DateTime.UtcNow;

        await Assert.ThrowsAsync<ArgumentException>(() => new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", now, now));
    }

    [Fact]
    public async Task LoadAsync_HalfOpenRange_ExcludesToBoundary()
    {
        using var file = Csv(
            "open_time_utc,open,high,low,close\n" +
            "2026-01-01T00:00:00Z,100,110,90,100\n" +
            "2026-01-01T00:01:00Z,100,110,90,100\n" +
            "2026-01-01T00:02:00Z,100,110,90,100");

        var from = new DateTime(2026, 1, 1, 0, 1, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 1, 0, 2, 0, DateTimeKind.Utc);
        var result = await new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", from, to);

        Assert.Single(result);
        Assert.Equal(from, result[0].OpenTimeUtc);
    }

    [Fact]
    public async Task LoadAsync_DuplicateOpenTime_LastRowWins()
    {
        using var file = Csv(
            "open_time_utc,open,high,low,close\n" +
            "2026-01-01T00:00:00Z,100,110,90,101\n" +
            "2026-01-01T00:00:00Z,100,110,90,109");

        var result = await new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null);

        Assert.Single(result);
        Assert.Equal(109m, result[0].Close);
    }

    [Fact]
    public async Task LoadAsync_SemicolonSeparator_IsSupported()
    {
        using var file = Csv("open_time_utc;open;high;low;close;volume\n2026-01-01T00:00:00Z;100;110;90;105;2");
        var result = await new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null);

        Assert.Single(result);
        Assert.Equal(2m, result[0].Volume);
    }

    [Fact]
    public async Task LoadAsync_InvalidDecimal_ReportsRow()
    {
        using var file = Csv("open_time_utc,open,high,low,close\n2026-01-01T00:00:00Z,nope,110,90,100");
        var exception = await Assert.ThrowsAsync<FormatException>(() => new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null));

        Assert.Contains("row 2", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_InvalidOhlc_Throws()
    {
        using var file = Csv("open_time_utc,open,high,low,close\n2026-01-01T00:00:00Z,100,90,110,100");
        await Assert.ThrowsAsync<FormatException>(() => new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null));
    }

    [Fact]
    public async Task LoadAsync_NegativeVolume_Throws()
    {
        using var file = Csv("open_time_utc,open,high,low,close,volume\n2026-01-01T00:00:00Z,100,110,90,100,-1");
        await Assert.ThrowsAsync<FormatException>(() => new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null));
    }

    [Fact]
    public async Task LoadAsync_UnterminatedQuotedValue_Throws()
    {
        using var file = Csv("open_time_utc,open,high,low,close\n\"2026-01-01T00:00:00Z,100,110,90,100");
        await Assert.ThrowsAsync<FormatException>(() => new CsvHistoricalCandleSource(file.Path).LoadAsync("BTCUSDC", "1m", null, null));
    }

    private static TempCsv Csv(string content) => new(content);

    private sealed class TempCsv : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".csv");

        public TempCsv(string content) => File.WriteAllText(Path, content);
        public void Dispose() => File.Delete(Path);
    }
}
