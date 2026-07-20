using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingSystem.Application.MarketData;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Indicators.Abstractions;
using TradingSystem.Redis.Messaging;
namespace IndicatorServices;
public sealed class Worker(IEnumerable<IIndicatorProcessor> processors, IHistoricalCandleSource history, IConnectionMultiplexer redis, IRedisStatePublisher publisher, TimeProvider time, ILogger<Worker> logger) : BackgroundService
{
    readonly IIndicatorProcessor[] _processors = processors.ToArray();
    protected override async Task ExecuteAsync(CancellationToken ct) { foreach (var p in _processors) foreach (var s in p.Symbols.Distinct(StringComparer.OrdinalIgnoreCase)) foreach (var i in p.Intervals.Distinct(StringComparer.OrdinalIgnoreCase)) { var candles = await history.LoadLatestAsync(s, i, p.RequiredHistory, ct); p.Initialize(s, i, candles); logger.LogInformation("Indicator initialized. Indicator={Indicator}, Symbol={Symbol}, Interval={Interval}, Count={Count}", p.Name, s, i, candles.Count); } var pairs = _processors.SelectMany(p => p.Symbols.SelectMany(s => p.Intervals.Select(i => (s: s.ToUpperInvariant(), i: i.ToLowerInvariant())))).Distinct().ToArray(); var sub = redis.GetSubscriber(); foreach (var pair in pairs) { var channel = RedisChannels.Kline(pair.i, pair.s); await sub.SubscribeAsync(RedisChannel.Literal(channel), async (_, value) => await Handle(value!, ct)); logger.LogInformation("Indicator host subscribed. Channel={Channel}", channel); } try { await Task.Delay(Timeout.InfiniteTimeSpan, ct); } catch (OperationCanceledException) when (ct.IsCancellationRequested) { } finally { foreach (var pair in pairs) await sub.UnsubscribeAsync(RedisChannel.Literal(RedisChannels.Kline(pair.i, pair.s))); } }
    async Task Handle(string json, CancellationToken ct) { try { var m = JsonSerializer.Deserialize<ClosedKlineMessage>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); if (m is null || !KlineMessageMapper.TryMap(m, out var c)) { logger.LogWarning("Invalid closed kline message."); return; } foreach (var p in _processors.Where(x => x.Symbols.Contains(c.Symbol, StringComparer.OrdinalIgnoreCase) && x.Intervals.Contains(c.Interval, StringComparer.OrdinalIgnoreCase))) { var snapshot = p.Process(c, time.GetUtcNow()); if (snapshot is null) continue; await publisher.SetAndPublishAsync($"indicator_state:{p.Name}:{c.Symbol}:{c.Interval}", RedisChannels.Indicator(p.Name), snapshot, ct); } } catch (Exception ex) { logger.LogError(ex, "Indicator processing failed."); } }
}
