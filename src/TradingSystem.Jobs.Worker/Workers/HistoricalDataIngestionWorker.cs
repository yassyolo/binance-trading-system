using Microsoft.Extensions.Options;
using TradingSystem.Application.MarketData;
using TradingSystem.Domain.MarketData;
using TradingSystem.JobOrchestration.Contracts;
using TradingSystem.JobOrchestration.Models;
using TradingSystem.Jobs.Worker.Configuration;

namespace TradingSystem.Jobs.Worker.Workers;

public sealed class HistoricalDataIngestionWorker(
	IHistoricalCandleRangeSource source,
	IHistoricalMarketDataStore store,
	IOptions<HistoricalDataIngestionOptions> options,
	ILogger<HistoricalDataIngestionWorker> logger) 
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var settings = options.Value;
		if (!settings.Enabled)
			return;

		var symbols = settings.Symbols
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim().ToUpperInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var intervals = settings.Intervals
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim().ToLowerInvariant())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		logger.LogInformation(
			"Historical ingestion effective configuration. Symbols = {Symbols}; Intervals = {Intervals}; LookbackDays = {LookbackDays}; OverlapCandles = {OverlapCandles}",
			string.Join(',', symbols),
			string.Join(',', intervals),
			settings.InitialLookbackDays,
			settings.OverlapCandles);

		var pollDelay = TimeSpan.FromMinutes(Math.Max(1, settings.PollMinutes));

		while (!stoppingToken.IsCancellationRequested)
		{
			foreach (var symbol in symbols)
			{
				foreach (var interval in intervals)
				{
					try
					{
						await IngestAsync(
							symbol,
							interval,
							settings,
							stoppingToken);
					}
					catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
					{
						return;
					}
					catch (Exception exception)
					{
						logger.LogError(
							exception,
							"Historical ingestion failed for {Symbol} {Interval}. The worker will continue.",
							symbol,
							interval);
					}
				}
			}

			try
			{
				await Task.Delay(pollDelay, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
		}
	}

	private async Task IngestAsync(
		string symbol,
		string interval,
		HistoricalDataIngestionOptions settings,
		CancellationToken ct)
	{
		var duration = ParseInterval(interval);
		var nowUtc = DateTime.UtcNow;
		var latest = await store.GetLatestOpenTimeAsync(symbol, interval, ct);
		var overlap = TimeSpan.FromTicks(
			checked(duration.Ticks * Math.Max(1, settings.OverlapCandles)));

		var from = latest?.Subtract(overlap)
			?? nowUtc.AddDays(-settings.InitialLookbackDays);
		var to = nowUtc;

		var downloaded = await source.LoadAsync(
			symbol,
			interval,
			from,
			to,
			ct);

		// Binance includes the currently forming candle. Persist only candles whose
		// close boundary has passed so backtests remain deterministic.
		var closed = downloaded
			.Where(x => x.OpenTimeUtc + duration <= nowUtc)
			.OrderBy(x => x.OpenTimeUtc)
			.ToArray();

		await store.UpsertCandlesAsync(closed, ct);

		var all = await store.LoadCandlesAsync(
			symbol,
			interval,
			from,
			to,
			ct);

		var gaps = DetectGaps(symbol, interval, all, duration);
		await store.ReplaceGapsAsync(symbol, interval, gaps, ct);

		logger.LogInformation(
			"Historical data {Symbol} {Interval}: requested {FromUtc:o} - {ToUtc:o}, downloaded {Downloaded}, persisted closed {Persisted}, first {FirstUtc:o}, last {LastUtc:o}, gaps {Gaps}",
			symbol,
			interval,
			from,
			to,
			downloaded.Count,
			closed.Length,
			closed.FirstOrDefault()?.OpenTimeUtc,
			closed.LastOrDefault()?.OpenTimeUtc,
			gaps.Count);
	}

	public static IReadOnlyList<HistoricalDataGap> DetectGaps(
		string symbol,
		string interval,
		IReadOnlyList<MarketCandle> candles,
		TimeSpan duration)
	{
		if (duration <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(duration));

		var ordered = candles
			.OrderBy(x => x.OpenTimeUtc)
			.ToArray();

		var gaps = new List<HistoricalDataGap>();

		for (var i = 1; i < ordered.Length; i++)
		{
			var expected = ordered[i - 1].OpenTimeUtc + duration;
			if (ordered[i].OpenTimeUtc <= expected)
				continue;

			var missing = checked((int)(
				(ordered[i].OpenTimeUtc - expected).Ticks /
				duration.Ticks));

			gaps.Add(new HistoricalDataGap(
				symbol,
				interval,
				expected,
				ordered[i].OpenTimeUtc - duration,
				missing));
		}

		return gaps;
	}

	public static TimeSpan ParseInterval(string value)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(value);

		if (value.Length < 2 ||
			!int.TryParse(value[..^1], out var amount) ||
			amount <= 0)
		{
			throw new ArgumentException(
				$"Invalid interval '{value}'.",
				nameof(value));
		}

		return value[^1] switch
		{
			'm' => TimeSpan.FromMinutes(amount),
			'h' => TimeSpan.FromHours(amount),
			'd' => TimeSpan.FromDays(amount),
			_ => throw new ArgumentOutOfRangeException(
				nameof(value),
				"Supported intervals use m, h or d.")
		};
	}
}
