using System.Globalization;
using System.Text.Json;
using AlligatorIndicatorService.Configuration;
using AlligatorIndicatorService.Models;
using AlligatorIndicatorService.Services;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Redis;

namespace AlligatorIndicatorService;

public sealed class Worker(
    ILogger<Worker> logger,
    IConnectionMultiplexer redis,
    BinanceHistoricalKlineClient historicalClient,
    AlligatorMaEngine engine,
    IOptions<AlligatorOptions> options) 
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };


    private readonly AlligatorOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = Normalize(options.Symbols, x => x.ToUpperInvariant());
        var intervals = Normalize(options.Intervals, x => x.ToLowerInvariant());

        await InitializeAsync(symbols, intervals, stoppingToken);
        await SubscribeAsync(symbols, intervals, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task InitializeAsync(IReadOnlyCollection<string> symbols, IReadOnlyCollection<string> intervals, CancellationToken cancellationToken)
    {
        foreach (var symbol in symbols)
        {
            foreach (var interval in intervals)
            {
                var candles = await historicalClient.GetHistoricalCandlesAsync(
                    symbol,
                    interval,
                    options.HistoryLimit,
                    cancellationToken);

                engine.InitializeHistory(symbol, interval, candles);

                logger.LogInformation("Alligator initialized. Symbol={Symbol}, Interval={Interval}, Candles={Count}", symbol, interval, candles.Count);
            }
        }
    }

    private async Task SubscribeAsync(IReadOnlyCollection<string> symbols, IReadOnlyCollection<string> intervals, CancellationToken cancellationToken)
    {
        var subscriber = redis.GetSubscriber();

        foreach (var symbol in symbols)
        {
            foreach (var interval in intervals)
            {
                var channel = RedisChannels.Kline(interval, symbol);

                await subscriber.SubscribeAsync(RedisChannel.Literal(channel),
                    async (_, message) =>
                    {
                        await HandleKlineMessageAsync(message!, cancellationToken);
                    });

                logger.LogInformation("Subscribed to {Channel}", channel);
            }
        }
    }

    private async Task HandleKlineMessageAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<ClosedKlineMessage>(json);

            if (message is null || !TryBuildCandle(message, out var candle))
            {
                logger.LogWarning("Invalid kline message. Json={Json}", json);
                return;
            }

            var symbol = message.Symbol.ToUpperInvariant();
            var interval = message.Interval.ToLowerInvariant();

            var payload = engine.Process(symbol, interval, candle);

            if (payload is null)
                return;

            var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

            var db = redis.GetDatabase();
            var stateKey = RedisKeys.AlligatorState(symbol, interval);

            await db.StringSetAsync(stateKey, payloadJson);
            await db.PublishAsync(RedisChannel.Literal(RedisChannels.AlligatorMa), payloadJson);

            logger.LogInformation("Alligator published. Symbol={Symbol}, Interval={Interval}", symbol, interval);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Alligator kline message.");
        }
    }

    private static bool TryBuildCandle(ClosedKlineMessage message, out Candle candle)
    {
        candle = default!;

        if (!TryParseDecimal(message.Open, out var open) ||
            !TryParseDecimal(message.High, out var high) ||
            !TryParseDecimal(message.Low, out var low) ||
            !TryParseDecimal(message.Close, out var close))
        {
            return false;
        }

        candle = new Candle
        {
            Time = message.Time,
            CloseTime = message.CloseTime,
            Open = open,
            High = high,
            Low = low,
            Close = close
        };

        return true;
    }

    private static bool TryParseDecimal(string? value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

    private static string[] Normalize(IEnumerable<string> values, Func<string, string> normalize)
        => values.Select(x => normalize(x.Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToArray();
}