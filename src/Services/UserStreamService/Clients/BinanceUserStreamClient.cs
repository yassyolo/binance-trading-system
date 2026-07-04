using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using UserStreamService.Services;

namespace UserStreamService.Clients;

public sealed class BinanceUserStreamClient(
    IConfiguration configuration,
    UserStreamProcessor processor,
    ILogger<BinanceUserStreamClient> logger)
{
    public async Task RunAsync(string listenKey, Func<Task>? onConnected, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["Binance:UserStreamWebSocketUrl"] ?? "wss://fstream.binance.com/private/ws";

        var url = $"{baseUrl}?listenKey={Uri.EscapeDataString(listenKey)}";

        using var socket = new ClientWebSocket();

        logger.LogInformation("Connecting to Binance user stream websocket.");

        await socket.ConnectAsync(new Uri(url), cancellationToken);

        logger.LogInformation("Connected to Binance user stream websocket.");

        await SubscribeAsync(socket, cancellationToken);

        if (onConnected is not null)
            await onConnected();

        await ReceiveLoopAsync(socket, cancellationToken);
    }

    private async Task SubscribeAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var subscribeMessage = JsonSerializer.Serialize(new
        {
            method = "SUBSCRIBE",
            @params = new[] { "!userDataStream" },
            id = 1
        });

        var bytes = Encoding.UTF8.GetBytes(subscribeMessage);

        await socket.SendAsync(
            bytes,
            WebSocketMessageType.Text,
            true,
            cancellationToken);

        logger.LogInformation("Sent SUBSCRIBE for !userDataStream.");
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 32];

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = new StringBuilder();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogWarning("Binance user stream websocket closed by remote server.");
                    return;
                }

                message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            await processor.ProcessAsync(message.ToString());
        }
    }
}