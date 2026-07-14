using System.Net.WebSockets;
using System.Text.Json;
using MarketDataService.Configuration;
using MarketDataService.Services;
using Microsoft.Extensions.Options;
using TradingSystem.Infrastructure.WebSockets;

namespace MarketDataService;

public sealed class Worker(
    ILogger<Worker> logger,
    IOptions<MarketDataOptions> options,
    KlinePublisher publisher)
    : BackgroundService
{
    private readonly MarketDataOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var symbols = Normalize(_options.Symbols, static value => value.ToUpperInvariant());
        var intervals = Normalize(_options.Intervals, static value => value.ToLowerInvariant());

        logger.LogInformation(
            "Starting MarketDataService. Symbols={Symbols}, Intervals={Intervals}",
            string.Join(',', symbols),
            string.Join(',', intervals));

        var loops = intervals
            .Select(interval => RunIntervalLoopAsync(symbols, interval, stoppingToken))
            .ToArray();

        await Task.WhenAll(loops);
    }

    private async Task RunIntervalLoopAsync(
        IReadOnlyCollection<string> symbols,
        string interval,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var url = BuildUrl(symbols, interval);
                var socketOptions = new WebSocketOptions
                {
                    KeepAliveIntervalSeconds = _options.KeepAliveIntervalSeconds,
                    ReceiveBufferSizeBytes = _options.ReceiveBufferSizeBytes
                };

                using var socket = ClientWebSocketFactory.Create(socketOptions);

                logger.LogInformation("Connecting to Binance market stream. Interval={Interval}, Url={Url}", interval, url);
                await socket.ConnectAsync(new Uri(url), stoppingToken);
                logger.LogInformation("Connected to Binance market stream. Interval={Interval}", interval);

                await ReceiveLoopAsync(socket, socketOptions.ReceiveBufferSizeBytes, interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Market WebSocket loop failed. Interval={Interval}", interval);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Reconnecting market stream. Interval={Interval}, DelaySeconds={DelaySeconds}",
                    interval,
                    _options.ReconnectDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task ReceiveLoopAsync(
        ClientWebSocket socket,
        int bufferSize,
        string interval,
        CancellationToken cancellationToken)
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var json = await WebSocketMessageReader.ReadTextMessageAsync(
                socket,
                bufferSize,
                cancellationToken);

            if (json is null)
            {
                logger.LogWarning("Binance market stream closed remotely. Interval={Interval}", interval);
                return;
            }

            if (string.IsNullOrWhiteSpace(json))
                continue;

            await ProcessMessageAsync(json, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("k", out var kline) ||
                !kline.TryGetProperty("x", out var closed) ||
                closed.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !closed.GetBoolean())
            {
                return;
            }

            await publisher.PublishClosedKlineAsync(kline, cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid Binance market JSON message.");
        }
    }

    private string BuildUrl(IReadOnlyCollection<string> symbols, string interval)
    {
        var streams = string.Join('/', symbols.Select(symbol => $"{symbol.ToLowerInvariant()}@kline_{interval}"));
        return $"{_options.BinanceWebSocketBaseUrl.TrimEnd('/', '?')}?streams={streams}";
    }

    private static string[] Normalize(IEnumerable<string>? values, Func<string, string> normalize)
        => values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => normalize(value.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];
}
