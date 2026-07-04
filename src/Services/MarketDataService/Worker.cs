using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MarketDataService.Configuration;
using MarketDataService.Services;
using Microsoft.Extensions.Options;

namespace MarketDataService;

public sealed class Worker(
    ILogger<Worker> logger,
    IOptions<MarketDataOptions> options,
    KlinePublisher klinePublisher)
    : BackgroundService
{
    private readonly MarketDataOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = _options.Symbols.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();

        var intervals = _options.Intervals.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        logger.LogInformation("Starting MarketDataService. Symbols={Symbols}, Intervals={Intervals}", string.Join(",", symbols), string.Join(",", intervals));

        var tasks = intervals.Select(interval => RunWebSocketLoopAsync(symbols, interval, stoppingToken));

        await Task.WhenAll(tasks);
    }

    private async Task RunWebSocketLoopAsync(
        string[] symbols,
        string interval,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var url = BuildWebSocketUrl(symbols, interval);

                using var socket = new ClientWebSocket();

                logger.LogInformation("Connecting to Binance kline websocket. Interval={Interval}, Url={Url}", interval, url);

                await socket.ConnectAsync(new Uri(url), stoppingToken);

                logger.LogInformation("Connected to Binance websocket. Interval={Interval}", interval);

                await ReceiveLoopAsync(socket, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebSocket loop failed. Interval={Interval}", interval);
            }

            logger.LogWarning("Reconnecting websocket. Interval={Interval}, DelaySeconds={Delay}", interval, _options.ReconnectDelaySeconds);

            await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
        }
    }

    private string BuildWebSocketUrl(string[] symbols, string interval)
    {
        var streams = string.Join("/", symbols.Select(symbol => $"{symbol.ToLowerInvariant()}@kline_{interval.ToLowerInvariant()}"));

        return $"{_options.BinanceWebSocketBaseUrl}?streams={streams}";
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
                    logger.LogWarning("Binance websocket closed by remote server.");
                    return;
                }

                message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            await ProcessMessageAsync(message.ToString());
        }
    }

    private async Task ProcessMessageAsync(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("data", out var data))
                return;

            if (!data.TryGetProperty("k", out var kline))
                return;

            var isClosed = kline.GetProperty("x").GetBoolean();

            if (!isClosed)
                return;

            await klinePublisher.PublishClosedKlineAsync(kline);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid Binance websocket JSON message.");
        }
    }
}