using System.Text.Json;
using BollingerIndicatorService.Models;
using BollingerIndicatorService.Services;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;

namespace BollingerIndicatorService;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConnectionMultiplexer _redis;
    private readonly BinanceHistoricalKlineClient _historicalClient;
    private readonly BollingerEngine _engine;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        BinanceHistoricalKlineClient historicalClient,
        BollingerEngine engine)
    {
        _logger = logger;
        _configuration = configuration;
        _redis = redis;
        _historicalClient = historicalClient;
        _engine = engine;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration.GetSection("Bollinger:Symbols").Get<string[]>() ?? ["BTCUSDC"];
        var intervals = _configuration.GetSection("Bollinger:Intervals").Get<string[]>() ?? ["30m", "1h"];
        var historyLimit = _configuration.GetValue<int>("Bollinger:HistoryLimit", 200);

        foreach (var symbol in symbols)
        {
            foreach (var interval in intervals)
            {
                var candles = await _historicalClient.GetHistoricalCandlesAsync(
                    symbol,
                    interval,
                    historyLimit,
                    stoppingToken);

                _engine.InitializeHistory(symbol, interval, candles);

                _logger.LogInformation(
                    "Bollinger history initialized. Symbol={Symbol}, Interval={Interval}, Candles={Count}",
                    symbol,
                    interval,
                    candles.Count);
            }
        }

        var subscriber = _redis.GetSubscriber();

        foreach (var symbol in symbols)
        {
            foreach (var interval in intervals)
            {
                var channel = RedisNames.KlineChannel(interval, symbol);

                await subscriber.SubscribeAsync(
                    RedisChannel.Literal(channel),
                    async (_, message) =>
                    {
                        await HandleKlineMessageAsync(message!, stoppingToken);
                    });

                _logger.LogInformation("Subscribed to channel {Channel}", channel);
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleKlineMessageAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var symbol = root.GetProperty("symbol").GetString()!.ToUpperInvariant();
            var interval = root.GetProperty("interval").GetString()!.ToLowerInvariant();

            var candle = new BollingerCandle
            {
                Time = root.GetProperty("time").GetInt64(),
                CloseTime = root.GetProperty("close_time").GetInt64(),
                Open = decimal.Parse(root.GetProperty("open").GetString()!),
                Close = decimal.Parse(root.GetProperty("close").GetString()!)
            };

            var payload = _engine.Process(symbol, interval, candle);

            if (payload is null)
                return;

            var payloadJson = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

            var db = _redis.GetDatabase();

            var stateKey = $"indicator_state:bb:{symbol}:{interval}";

            await db.StringSetAsync(stateKey, payloadJson);
            await db.PublishAsync(
                RedisChannel.Literal("indicator_channel:bb"),
                payloadJson);

            _logger.LogInformation(
                "Bollinger published. Symbol={Symbol}, Interval={Interval}, StateKey={StateKey}",
                symbol,
                interval,
                stateKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Bollinger kline message.");
        }
    }
}