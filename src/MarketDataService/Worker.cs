using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Infrastructure.WebSockets;

namespace MarketDataService;
public sealed class Worker(
    IOptions<MarketDataOptions> options, 
    KlinePublisher publisher, 
    ILogger<Worker> logger)
    :BackgroundService
{
    readonly MarketDataOptions o = options.Value;
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var symbols = o.Symbols.Where(x => ! string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpperInvariant()).Distinct().ToArray();
        var intervals = o.Intervals.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();
        await Task.WhenAll(intervals.Select(i => Loop(symbols, i, ct)));
    }
    async Task Loop(string[] symbols, string interval, CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try
            {
                using var ws = ClientWebSocketFactory.Create(new(){KeepAliveIntervalSeconds = o.KeepAliveIntervalSeconds, ReceiveBufferSizeBytes = o.ReceiveBufferSizeBytes});
                var streams = string.Join('/', symbols.Select(s => $"{s.ToLowerInvariant()}@kline_{interval}"));
                var url = $"{o.BinanceWebSocketBaseUrl.TrimEnd('/', '?')}?streams = {streams}";
                await ws.ConnectAsync(new Uri(url), ct);
                while(ws.State==WebSocketState.Open && !ct.IsCancellationRequested)
                    {
                    var json = await WebSocketMessageReader.ReadTextMessageAsync(ws, o.ReceiveBufferSizeBytes, ct);
                    if(json is null)break;using var d = JsonDocument.Parse(json);
                    if(d.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("k", out var k) && k.TryGetProperty("x", out var x) && x.ValueKind==JsonValueKind.True)
                        await publisher.PublishAsync(k, ct);
                }
            }
            catch(OperationCanceledException)
            when(ct.IsCancellationRequested)
            {
                break;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Market stream failed. Interval = {Interval}", interval);
            }
            if(!ct.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(o.ReconnectDelaySeconds), ct);
        }
    }
}
