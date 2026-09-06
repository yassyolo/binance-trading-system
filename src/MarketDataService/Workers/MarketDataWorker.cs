using System.Net.WebSockets;
using System.Text.Json;
using MarketDataService.Configuration;
using MarketDataService.Services;
using Microsoft.Extensions.Options;
using TradingSystem.Infrastructure.WebSockets;

namespace MarketDataService.Workers;

public sealed class MarketDataWorker(
    IOptions<MarketDataOptions> options,
    KlinePublisher klinePublisher,
    ILogger<MarketDataWorker> logger)
    : BackgroundService
{
    private readonly MarketDataOptions _options = options.Value;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        var symbols = _options.Symbols.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var intervals = _options.Intervals.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.WhenAll(intervals.Select(x => RunStreamLoopAsync(symbols, x, ct)));
    }

    private async Task RunStreamLoopAsync(IReadOnlyCollection<string> symbols, string interval, CancellationToken ct)
    {
        var failureCount = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var webSocket = ClientWebSocketFactory.Create(new()
                {
                    KeepAliveIntervalSeconds = _options.KeepAliveIntervalSeconds
                });

                var streams = string.Join('/', symbols.Select(x => $"{x.ToLowerInvariant()}@kline_{interval}"));
                var baseUrl = _options.BinanceWebSocketBaseUrl.TrimEnd('/', '?');
                var streamUrl = new Uri($"{baseUrl}?streams={streams}");

                logger.LogInformation("Connecting to Binance kline stream. Interval = {Interval}, Symbols = {Symbols}", interval, string.Join(',', symbols));

                await webSocket.ConnectAsync(streamUrl, ct);
                
                failureCount = 0;

                while (webSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var json = await WebSocketMessageReader.ReadTextMessageAsync(webSocket, _options.ReceiveBufferSizeBytes, ct);
                    if (json is null)
                        break;

                    try
                    {
                        using var document = JsonDocument.Parse(json);
                        if (document.RootElement.TryGetProperty("data", out var data) &&
                            data.TryGetProperty("k", out var kline) &&
                            kline.TryGetProperty("x", out var isClosed) &&
                            isClosed.ValueKind == JsonValueKind.True)
                        {
                            await klinePublisher.PublishAsync(kline, ct);
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        logger.LogWarning(jsonEx, "Invalid Binance websocket JSON. Interval = {Interval}", interval);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                failureCount++;
                
                logger.LogError(ex, "Market stream failed. Interval = {Interval}, ConsecutiveFailures = {Failures}", interval, failureCount);
            }

            if (ct.IsCancellationRequested)
                break;

            var exponentialSeconds = _options.ReconnectDelaySeconds * Math.Pow(2, Math.Min(failureCount, 6));
            var boundedSeconds = Math.Min(_options.MaximumReconnectDelaySeconds, exponentialSeconds);
            var jitterMilliseconds = Random.Shared.Next(0, 1_000);

            await Task.Delay(TimeSpan.FromSeconds(boundedSeconds) + TimeSpan.FromMilliseconds(jitterMilliseconds), ct);
        }
    }
}
