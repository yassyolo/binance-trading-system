using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;

namespace MarketDataService;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;

    public Worker(
        ILogger<Worker> logger,
        IConnectionMultiplexer redis,
        IConfiguration configuration)
    {
        _logger = logger;
        _redis = redis;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = _configuration
            .GetSection("MarketData:Symbols")
            .Get<string[]>() ?? ["BTCUSDC"];

        var intervals = _configuration
            .GetSection("MarketData:Intervals")
            .Get<string[]>() ?? ["1m"];

        _logger.LogInformation(
            "Starting MarketDataService. Symbols={Symbols}, Intervals={Intervals}",
            string.Join(",", symbols),
            string.Join(",", intervals));

        var tasks = intervals.Select(interval =>
            RunWebSocketLoopAsync(symbols, interval, stoppingToken));

        await Task.WhenAll(tasks);
    }

    private async Task RunWebSocketLoopAsync(
        string[] symbols,
        string interval,
        CancellationToken stoppingToken)
    {
        var reconnectDelaySeconds =
            _configuration.GetValue<int>("MarketData:ReconnectDelaySeconds", 5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var url = BuildWebSocketUrl(symbols, interval);

                using var socket = new ClientWebSocket();

                _logger.LogInformation(
                    "Connecting to Binance kline websocket. Interval={Interval}, Url={Url}",
                    interval,
                    url);

                await socket.ConnectAsync(new Uri(url), stoppingToken);

                _logger.LogInformation(
                    "Connected to Binance websocket. Interval={Interval}",
                    interval);

                await ReceiveLoopAsync(socket, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "WebSocket loop failed. Interval={Interval}",
                    interval);
            }

            _logger.LogWarning(
                "Reconnecting websocket. Interval={Interval}, DelaySeconds={Delay}",
                interval,
                reconnectDelaySeconds);

            await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), stoppingToken);
        }
    }

    private string BuildWebSocketUrl(string[] symbols, string interval)
    {
        var baseUrl = _configuration["MarketData:BinanceWebSocketBaseUrl"]
            ?? "wss://fstream.binance.com/market/stream";

        var streams = string.Join(
            "/",
            symbols.Select(symbol => $"{symbol.ToLowerInvariant()}@kline_{interval.ToLowerInvariant()}"));

        return $"{baseUrl}?streams={streams}";
    }

    private async Task ReceiveLoopAsync(
        ClientWebSocket socket,
        CancellationToken stoppingToken)
    {
        var buffer = new byte[1024 * 16];

        while (socket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
        {
            var message = new StringBuilder();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, stoppingToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("Binance websocket closed by remote server.");
                    return;
                }

                var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                message.Append(chunk);
            }
            while (!result.EndOfMessage);

            await ProcessMessageAsync(message.ToString());
        }
    }

    private async Task ProcessMessageAsync(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out var data))
            return;

        if (!data.TryGetProperty("k", out var kline))
            return;

        var isClosed = kline.GetProperty("x").GetBoolean();

        if (!isClosed)
            return;

        await HandleClosedKlineAsync(kline);
    }

    private async Task HandleClosedKlineAsync(JsonElement kline)
    {
        var symbol = kline.GetProperty("s").GetString()?.ToUpperInvariant();
        var interval = kline.GetProperty("i").GetString()?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(interval))
        {
            _logger.LogWarning("Invalid kline payload: {Payload}", kline.ToString());
            return;
        }

        var key = RedisNames.KlineKey(symbol, interval);
        var channel = RedisNames.KlineChannel(interval, symbol);

        var rawBinancePayload = kline.GetRawText();

        var cleanPayload = new
        {
            symbol,
            time = kline.GetProperty("t").GetInt64(),
            open = kline.GetProperty("o").GetString(),
            high = kline.GetProperty("h").GetString(),
            low = kline.GetProperty("l").GetString(),
            close = kline.GetProperty("c").GetString(),
            volume = kline.GetProperty("v").GetString(),
            close_time = kline.GetProperty("T").GetInt64(),
            interval
        };

        var cleanJson = JsonSerializer.Serialize(cleanPayload);

        var db = _redis.GetDatabase();

        await db.StringSetAsync(key, rawBinancePayload);
        await db.PublishAsync(RedisChannel.Literal(channel), cleanJson);

        _logger.LogInformation(
            "Closed kline published. Symbol={Symbol}, Interval={Interval}, Close={Close}, Key={Key}, Channel={Channel}",
            symbol,
            interval,
            kline.GetProperty("c").GetString(),
            key,
            channel);
    }
}