using System.Collections.Concurrent;
using System.Text.Json;
using IndicatorServices.Mapping;
using StackExchange.Redis;
using TradingSystem.Application.MarketData;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Indicators.Contracts;
using TradingSystem.Redis.Messaging.Contracts;

namespace IndicatorServices.Workers;

public sealed class IndicatorProcessingWorker(
	IEnumerable<IIndicatorProcessor> processors,
	IHistoricalCandleSource history,
	IConnectionMultiplexer redis,
	IRedisStatePublisher publisher,
	TimeProvider timeProvider,
	ILogger<IndicatorProcessingWorker> logger)
	: BackgroundService
{
	private static readonly JsonSerializerOptions MessageJsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly IIndicatorProcessor[] _processors = processors.ToArray();
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _processorLocks = new(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentDictionary<string, DateTime> _lastProcessedClose = new(StringComparer.OrdinalIgnoreCase);

	protected override async Task ExecuteAsync(CancellationToken ct)
	{
		foreach (var processor in _processors)
		{
			foreach (var symbol in processor.Symbols.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				foreach (var interval in processor.Intervals.Distinct(StringComparer.OrdinalIgnoreCase))
				{
					var normalizedSymbol = symbol.Trim().ToUpperInvariant();
					var normalizedInterval = interval.Trim().ToLowerInvariant();

					var candles = await history.LoadLatestAsync(normalizedSymbol, normalizedInterval, processor.RequiredHistory, ct);

					var closedCandles = candles.Where(x => x.IsClosed)
						.OrderBy(x => x.OpenTimeUtc)
						.ToArray();

					processor.Initialize(normalizedSymbol, normalizedInterval, closedCandles);

					if (closedCandles.Length > 0)
						_lastProcessedClose[Key(processor.Name, normalizedSymbol, normalizedInterval)] = closedCandles[^1].CloseTimeUtc;

					var stateKey = $"indicator_state:{processor.Name}:{normalizedSymbol}:{normalizedInterval}";
					var stateTtl = ResolveStateTtl(normalizedInterval);
					var expiryApplied = await redis.GetDatabase().KeyExpireAsync(stateKey, stateTtl);

					if (expiryApplied)
						logger.LogInformation("Indicator state TTL refreshed during startup. Key = {Key}, TTL = {Ttl}", stateKey, stateTtl);

					logger.LogInformation("Indicator initialized. Indicator = {Indicator}, Symbol = {Symbol}, Interval = {Interval}, Count = {Count}", processor.Name, normalizedSymbol, normalizedInterval, closedCandles.Length);
				}
			}
		}

		var subscriptions = _processors
			.SelectMany(processor => processor.Symbols.SelectMany(s =>
				processor.Intervals.Select(i => (
					Symbol: s.Trim().ToUpperInvariant(),
					Interval: i.Trim().ToLowerInvariant()))))
			.Distinct()
			.ToArray();

		var subscriber = redis.GetSubscriber();

		foreach (var subscription in subscriptions)
		{
			var channel = RedisChannels.Kline(subscription.Interval, subscription.Symbol);

			await subscriber.SubscribeAsync(RedisChannel.Literal(channel), async (_, value) => await HandleAsync(value.ToString(), ct));

			logger.LogInformation("Indicator host subscribed. Channel = {Channel}", channel);
		}

		try
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{ }
		finally
		{
			foreach (var subscription in subscriptions)
				await subscriber.UnsubscribeAsync(RedisChannel.Literal(RedisChannels.Kline(subscription.Interval, subscription.Symbol)));

			foreach (var gate in _processorLocks.Values)
				gate.Dispose();
		}
	}

	private async Task HandleAsync(string json, CancellationToken ct)
	{
		try
		{
			var message = JsonSerializer.Deserialize<ClosedKlineMessage>(json, MessageJsonOptions);

			if (message is null || !KlineMessageMapper.TryMap(message, out var candle))
			{
				logger.LogWarning("Invalid closed kline message.");
				return;
			}

			var matchingProcessors = _processors.Where(processor =>
				processor.Symbols.Contains(candle.Symbol, StringComparer.OrdinalIgnoreCase) &&
				processor.Intervals.Contains(candle.Interval, StringComparer.OrdinalIgnoreCase));

			foreach (var processor in matchingProcessors)
			{
				var key = Key(processor.Name, candle.Symbol, candle.Interval);
				var gate = _processorLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
				await gate.WaitAsync(ct);

				try
				{
					if (_lastProcessedClose.TryGetValue(key, out var lastClose) && candle.CloseTimeUtc <= lastClose)
					{
						logger.LogDebug("Duplicate or out-of-order candle ignored. Indicator = {Indicator}, Symbol = {Symbol}, Interval = {Interval}, CloseTime = {CloseTime}", processor.Name, candle.Symbol, candle.Interval, candle.CloseTimeUtc);

						continue;
					}

					var snapshot = processor.Process(candle, timeProvider.GetUtcNow());

					_lastProcessedClose[key] = candle.CloseTimeUtc;

					if (snapshot is null)
						continue;

					var stateKey = $"indicator_state:{processor.Name}:{candle.Symbol}:{candle.Interval}";
					await publisher.SetAndPublishAsync(stateKey, RedisChannels.Indicator(processor.Name), snapshot, ct);

					var stateTtl = ResolveStateTtl(candle.Interval);
					await redis.GetDatabase().KeyExpireAsync(stateKey, stateTtl);
				}
				finally
				{
					gate.Release();
				}
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{ }
		catch (JsonException exception)
		{
			logger.LogWarning(exception, "Invalid kline JSON received from Redis.");
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Indicator processing failed.");
		}
	}

	private static string Key(string indicator, string symbol, string interval)
		=> $"{indicator}:{symbol}:{interval}";

	private static TimeSpan ResolveStateTtl(string interval)
	{
		if (string.IsNullOrWhiteSpace(interval) || interval.Length < 2)
			return TimeSpan.FromMinutes(5);

		if (!int.TryParse(interval[..^1], out var amount) || amount <= 0)
			return TimeSpan.FromMinutes(5);

		var candleDuration = char.ToLowerInvariant(interval[^1]) switch
		{
			'm' => TimeSpan.FromMinutes(amount),
			'h' => TimeSpan.FromHours(amount),
			'd' => TimeSpan.FromDays(amount),
			_ => TimeSpan.FromMinutes(5)
		};

		var ttl = TimeSpan.FromTicks(candleDuration.Ticks * 3);
		
		return ttl < TimeSpan.FromMinutes(2) ? TimeSpan.FromMinutes(2) : ttl;
	}
}
