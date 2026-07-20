using System.Net.WebSockets;
using Microsoft.Extensions.Options;
using TradingSystem.Infrastructure.WebSockets;

namespace TradingSystem.Binance.UserStream;

public sealed class BinanceUserStreamClient(IOptions<BinanceUserStreamOptions> options):IBinanceUserStreamClient
{
    private readonly BinanceUserStreamOptions _o = options.Value;
    public async Task RunAsync(string key, Func<string, CancellationToken, Task> onMessage, Func<CancellationToken, Task>? connected, CancellationToken ct)
    {
        var wsOptions = new WebSocketOptions{KeepAliveIntervalSeconds = _o.KeepAliveIntervalSeconds, ReceiveBufferSizeBytes = _o.ReceiveBufferSizeBytes};using var socket = ClientWebSocketFactory.Create(wsOptions);
        var url = $"{_o.WebSocketBaseUrl.TrimEnd('/')}/ws/{Uri.EscapeDataString(key)}";await socket.ConnectAsync(new Uri(url), ct);if(connected is not null)await connected(ct);
        while(socket.State==WebSocketState.Open && !ct.IsCancellationRequested){var message = await WebSocketMessageReader.ReadTextMessageAsync(socket, wsOptions.ReceiveBufferSizeBytes, ct);if(message is null)return;if(!string.IsNullOrWhiteSpace(message))await onMessage(message, ct);}
    }
}
