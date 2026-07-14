using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Infrastructure.WebSockets;
using UserStreamService.Configuration;
using UserStreamService.Services;

namespace UserStreamService.Clients;

public sealed class BinanceUserStreamClient(
    IOptions<BinanceUserStreamOptions> binanceOptions,
    IOptions<UserStreamOptions> userStreamOptions,
    UserStreamProcessor processor,
    ILogger<BinanceUserStreamClient> logger)
{
    private readonly BinanceUserStreamOptions _binance = binanceOptions.Value;
    private readonly UserStreamOptions _stream = userStreamOptions.Value;

    public async Task RunAsync(
        string listenKey,
        Func<CancellationToken, Task>? connected,
        CancellationToken cancellationToken)
    {
        var url = $"{_binance.UserStreamWebSocketUrl.TrimEnd('?', '/')}?listenKey={Uri.EscapeDataString(listenKey)}";
        var socketOptions = new WebSocketOptions
        {
            KeepAliveIntervalSeconds = _stream.KeepAliveIntervalSeconds,
            ReceiveBufferSizeBytes = _stream.ReceiveBufferSizeBytes
        };

        using var socket = ClientWebSocketFactory.Create(socketOptions);

        logger.LogInformation("Connecting to Binance user stream.");
        await socket.ConnectAsync(new Uri(url), cancellationToken);
        logger.LogInformation("Connected to Binance user stream.");

        await SubscribeAsync(socket, cancellationToken);

        if (connected is not null)
            await connected(cancellationToken);

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = await WebSocketMessageReader.ReadTextMessageAsync(
                socket,
                socketOptions.ReceiveBufferSizeBytes,
                cancellationToken);

            if (message is null)
            {
                logger.LogWarning("Binance user stream closed remotely.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(message))
                await processor.ProcessAsync(message, cancellationToken);
        }
    }

    private static async Task SubscribeAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            method = "SUBSCRIBE",
            @params = new[] { "!userDataStream" },
            id = 1
        });

        await socket.SendAsync(
            Encoding.UTF8.GetBytes(payload),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }
}
