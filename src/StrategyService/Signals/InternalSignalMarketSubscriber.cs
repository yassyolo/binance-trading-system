using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Indicators;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Infrastructure.Serialization;
using TradingSystem.Signals.Configuration;
using TradingSystem.Signals.Contracts;
using TradingSystem.Signals.Models;
using TradingSystem.Signals.Models.Enums;

namespace StrategyService.Signals;

public sealed class InternalSignalMarketSubscriber(
    IConnectionMultiplexer redis,
    IOptions<SignalGenerationOptions> options,
    ISignalGenerationCoordinator coordinator,
    ILogger<InternalSignalMarketSubscriber> logger)
    : BackgroundService
{
    private sealed record MarketKey(string Symbol, string Interval);

    private sealed class JoinedState
    {
        public ClosedKlineMessage? Candle { get; set; }
        public long CandleCloseTime { get; set; }
        public Dictionary<string, decimal> Indicators { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }

    private static readonly string[] AlligatorIndicatorKeys =
    [
        "alligator_jaw",
        "alligator_teeth",
        "alligator_lips",
        "sma200"
    ];

    private static readonly string[] BollingerIndicatorKeys =
    [
        "BB20_CLOSE.upper",
        "BB20_CLOSE.middle",
        "BB20_CLOSE.lower"
    ];

    private readonly ConcurrentDictionary<MarketKey, JoinedState> _states = new();
    private readonly HashSet<MarketKey> _markets = options.Value.Bots
        .Where(x => x.Value.Enabled && x.Value.Mode != SignalGenerationMode.TradingViewOnly)
        .Select(x => new MarketKey(NormalizeSymbol(x.Value.Symbol), NormalizeInterval(x.Value.Interval)))
        .ToHashSet();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (_markets.Count == 0)
        {
            logger.LogInformation("Internal signal market subscriber has no enabled internal markets.");
            return;
        }

        var subscriber = redis.GetSubscriber();
        var subscriptions = new List<RedisChannel>();

        foreach (var market in _markets)
        {
            var channel = RedisChannel.Literal(RedisChannels.Kline(market.Interval, market.Symbol));
            await subscriber.SubscribeAsync(channel, async (_, value) =>
            {
                if (!value.HasValue || ct.IsCancellationRequested) return;
               
                await ProcessCandleSafelyAsync(value.ToString(), ct);
            });
            subscriptions.Add(channel);
        }

        foreach (var indicatorName in new[] { "alligator_ma", "bb" })
        {
            var channel = RedisChannel.Literal(RedisChannels.Indicator(indicatorName));
            await subscriber.SubscribeAsync(channel, async (_, value) =>
            {
                if (!value.HasValue || ct.IsCancellationRequested) return;
                
                await ProcessIndicatorSafelyAsync(value.ToString(), ct);
            });
            
            subscriptions.Add(channel);
        }

        logger.LogInformation("Internal signal market subscriber started. Markets = {Markets}; Channels = {Channels}", string.Join(", ", _markets.Select(x => $"{x.Symbol}:{x.Interval}")),  string.Join(", ", subscriptions));

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        finally
        {
            foreach (var channel in subscriptions)
            {
                try 
                { 
                    await subscriber.UnsubscribeAsync(channel); 
                }
                catch (Exception ex) 
                { 
                    logger.LogWarning(ex, "Could not unsubscribe from {Channel}", channel); 
                }
            }
        }
    }

    private async Task ProcessCandleSafelyAsync(string raw, CancellationToken ct)
    {
        try
        {
            var candle = JsonSerializer.Deserialize<ClosedKlineMessage>(raw, JsonDefaults.Messaging);
            if (candle is null) 
                return;

            var key = new MarketKey(NormalizeSymbol(candle.Symbol), NormalizeInterval(candle.Interval));
            if (!_markets.Contains(key)) 
                return;
           
            if (!TryParseCandle(candle, out _, out _, out _, out _, out _)) 
                return;

            var state = _states.GetOrAdd(key, _ => new JoinedState());
            await state.Gate.WaitAsync(ct);
            
            try
            {
                if (state.CandleCloseTime != candle.CloseTime)
                {
                    state.CandleCloseTime = candle.CloseTime;
                    state.Indicators.Clear();
                }

                state.Candle = candle;
                await TryDispatchAsync(key, state, ct);
            }
            finally
            {
                state.Gate.Release();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) 
        { 
            throw; 
        }
        catch (Exception ex) 
        { 
            logger.LogError(ex, "Internal signal candle processing failed."); 
        }
    }

    private async Task ProcessIndicatorSafelyAsync(string raw, CancellationToken ct)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<IndicatorSnapshotMessage>(raw, JsonDefaults.Messaging);
            if (snapshot is null) return;

            var key = new MarketKey(NormalizeSymbol(snapshot.Symbol), NormalizeInterval(snapshot.Timeframe));
            if (!_markets.Contains(key)) return;

            var state = _states.GetOrAdd(key, _ => new JoinedState());
            await state.Gate.WaitAsync(ct);
            try
            {
                if (state.CandleCloseTime != 0 && state.CandleCloseTime != snapshot.CandleCloseTime)
                    return;

                if (state.CandleCloseTime == 0)
                    state.CandleCloseTime = snapshot.CandleCloseTime;

                ReplaceIndicatorFamily(state.Indicators, snapshot.Indicators.Keys);

                foreach (var (name, value) in snapshot.Indicators)
                    state.Indicators[name] = value.Value;

                await TryDispatchAsync(key, state, ct);
            }
            finally
            {
                state.Gate.Release();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogError(ex, "Internal signal indicator processing failed."); }
    }

    private async Task TryDispatchAsync(MarketKey key, JoinedState state, CancellationToken ct)
    {
        var candle = state.Candle;
        if (candle is null || candle.CloseTime != state.CandleCloseTime) 
            return;
        
        if (!TryParseCandle(candle, out var open, out var high, out var low, out var close, out var volume)) 
            return;

        var snapshot = new MarketIndicatorSnapshot(
            key.Symbol,
            key.Interval,
            DateTimeOffset.FromUnixTimeMilliseconds(candle.Time).UtcDateTime,
            DateTimeOffset.FromUnixTimeMilliseconds(candle.CloseTime).UtcDateTime,
            open,
            high,
            low,
            close,
            volume,
            new Dictionary<string, decimal>(state.Indicators, StringComparer.OrdinalIgnoreCase));

        await coordinator.ProcessAsync(snapshot, ct);
    }

    private static void ReplaceIndicatorFamily(IDictionary<string, decimal> cachedIndicators, IEnumerable<string> incomingKeys)
    {
        var keys = incomingKeys.ToArray();

        if (keys.Any(k => AlligatorIndicatorKeys.Contains(k, StringComparer.OrdinalIgnoreCase)))
        {
            foreach (var key in AlligatorIndicatorKeys)
                cachedIndicators.Remove(key);
        }

        if (keys.Any(k => BollingerIndicatorKeys.Contains(k, StringComparer.OrdinalIgnoreCase)))
        {
            foreach (var key in BollingerIndicatorKeys)
                cachedIndicators.Remove(key);
        }
    }

    private static bool TryParseCandle(
        ClosedKlineMessage candle,
        out decimal open,
        out decimal high,
        out decimal low,
        out decimal close,
        out decimal volume)
    {
        open = 0;
        high = 0;
        low = 0;
        close = 0;
        volume = 0;

        if (!decimal.TryParse(candle.Open, NumberStyles.Any, CultureInfo.InvariantCulture, out open) || open <= 0) 
            return false;
        
        if (!decimal.TryParse(candle.High, NumberStyles.Any, CultureInfo.InvariantCulture, out high) || high <= 0) 
            return false;
        
        if (!decimal.TryParse(candle.Low, NumberStyles.Any, CultureInfo.InvariantCulture, out low) || low <= 0) 
            return false;
       
        if (!decimal.TryParse(candle.Close, NumberStyles.Any, CultureInfo.InvariantCulture, out close) || close <= 0) 
            return false;
        
        if (!decimal.TryParse(candle.Volume, NumberStyles.Any, CultureInfo.InvariantCulture, out volume) || volume < 0) 
            return false;

        return candle.Time > 0 && candle.CloseTime >= candle.Time;
    }

    private static string NormalizeSymbol(string value) 
        => value.Trim().ToUpperInvariant();
    
    private static string NormalizeInterval(string value) 
        => value.Trim().ToLowerInvariant();
}
