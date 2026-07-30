using Microsoft.Extensions.Options;
using TradingSystem.Application.MarketData;
using TradingSystem.Domain.MarketData;
using TradingSystem.JobOrchestration;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class HistoricalDataIngestionWorker(
    IHistoricalCandleRangeSource source,
    IHistoricalMarketDataStore store,
    IOptions<HistoricalDataIngestionOptions> options,
    ILogger<HistoricalDataIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, settings.PollMinutes)));
        try
        {
            do
            {
                foreach (var symbol in settings.Symbols)
                    foreach (var interval in settings.Intervals)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        try
                        {
                            await IngestAsync(symbol, interval, settings, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            logger.LogError(exception, "Historical ingestion failed for {Symbol} {Interval}", symbol, interval);
                        }
                    }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal hosted-service shutdown.
        }
    }

    private async Task IngestAsync(string symbol, string interval, HistoricalDataIngestionOptions settings, CancellationToken ct)
    {
        var duration = ParseInterval(interval);
        var latest = await store.GetLatestOpenTimeAsync(symbol, interval, ct);
        var overlap = TimeSpan.FromTicks(checked(duration.Ticks * Math.Max(1, settings.OverlapCandles)));
        var from = latest?.Subtract(overlap) ?? DateTime.UtcNow.AddDays(-settings.InitialLookbackDays);
        var to = DateTime.UtcNow;

        var candles = await source.LoadAsync(symbol, interval, from, to, ct);
        await store.UpsertCandlesAsync(candles, ct);
        var all = await store.LoadCandlesAsync(symbol, interval, from, to, ct);
        var gaps = DetectGaps(symbol, interval, all, duration);
        await store.ReplaceGapsAsync(symbol, interval, gaps, ct);

        logger.LogInformation("Historical data {Symbol} {Interval}: upserted {Count}, gaps {Gaps}", symbol, interval, candles.Count, gaps.Count);
    }

    public static IReadOnlyList<HistoricalDataGap> DetectGaps(string symbol, string interval, IReadOnlyList<MarketCandle> candles, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));

        var ordered = candles.OrderBy(x => x.OpenTimeUtc).ToArray();
        var gaps = new List<HistoricalDataGap>();
        for (var i = 1; i < ordered.Length; i++)
        {
            var expected = ordered[i - 1].OpenTimeUtc + duration;
            if (ordered[i].OpenTimeUtc <= expected)
                continue;

            var missing = checked((int)((ordered[i].OpenTimeUtc - expected).Ticks / duration.Ticks));
            gaps.Add(new HistoricalDataGap(symbol, interval, expected, ordered[i].OpenTimeUtc - duration, missing));
        }

        return gaps;
    }

    public static TimeSpan ParseInterval(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length < 2 || !int.TryParse(value[..^1], out var amount) || amount <= 0)
            throw new ArgumentException($"Invalid interval '{value}'.", nameof(value));

        return value[^1] switch
        {
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            'd' => TimeSpan.FromDays(amount),
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Supported intervals use m, h or d.")
        };
    }
}
