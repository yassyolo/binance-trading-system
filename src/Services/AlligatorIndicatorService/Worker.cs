using System.Text.Json;
using AlligatorIndicatorService.Models;
using AlligatorIndicatorService.Services;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;

namespace AlligatorIndicatorService;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConnectionMultiplexer _redis;
    private readonly BinanceHistoricalKlineClient _historicalClient;
    private readonly AlligatorMaEngine _engine;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        BinanceHistoricalKlineClient historicalClient,
        AlligatorMaEngine engine)
    {
        _logger = logger;
        _configuration = configuration;
        _redis = redis;
        _historicalClient = historicalClient;
        _engine = engine;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration.GetSection("Alligator:Symbols").Get<string[]>() ?? ["BTCUSDC"];
        var intervals = _configuration.GetSection("Alligator:Intervals").Get<string[]>() ?? ["5m"];
        var historyLimit = _configuration.GetValue<int>("Alligator:HistoryLimit", 300);

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
                    "Alligator history initialized. Symbol={Symbol}, Interval={Interval}, Candles={Count}",
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
                var channel = RedisChannels.Kline(interval, symbol);

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

            var candle = new Candle
            {
                Time = root.GetProperty("time").GetInt64(),
                CloseTime = root.GetProperty("close_time").GetInt64(),
                Open = decimal.Parse(root.GetProperty("open").GetString()!),
                High = decimal.Parse(root.GetProperty("high").GetString()!),
                Low = decimal.Parse(root.GetProperty("low").GetString()!),
                Close = decimal.Parse(root.GetProperty("close").GetString()!)
            };

            var payload = _engine.Process(symbol, interval, candle);

            if (payload is null)
            {
                _logger.LogInformation(
                    "Not enough candles for Alligator/SMA200. Symbol={Symbol}, Interval={Interval}",
                    symbol,
                    interval);
                return;
            }

            var payloadJson = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

            var db = _redis.GetDatabase();

            var stateKey = $"indicator_state:alligator_ma:{symbol}:{interval}";

            await db.StringSetAsync(stateKey, payloadJson);
            await db.PublishAsync(
                RedisChannel.Literal(RedisChannels.AlligatorMa),
                payloadJson);

            _logger.LogInformation(
                "Alligator published. Symbol={Symbol}, Interval={Interval}, StateKey={StateKey}",
                symbol,
                interval,
                stateKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Alligator kline message.");
        }
    }
}