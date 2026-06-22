using System.Net.WebSockets;
using System.Text;

namespace UserStreamService.Clients;

public sealed class BinanceUserStreamClient
{
    private readonly IConfiguration _configuration;
    private readonly UserStreamService.Services.UserStreamProcessor _processor;
    private readonly ILogger<BinanceUserStreamClient> _logger;

    public BinanceUserStreamClient(
        IConfiguration configuration,
        UserStreamService.Services.UserStreamProcessor processor,
        ILogger<BinanceUserStreamClient> logger)
    {
        _configuration = configuration;
        _processor = processor;
        _logger = logger;
    }

    public async Task RunAsync(string listenKey, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Binance:UserStreamWebSocketUrl"]
            ?? "wss://fstream.binance.com/private/ws";

        var url = $"{baseUrl}?listenKey={Uri.EscapeDataString(listenKey)}";

        using var socket = new ClientWebSocket();

        _logger.LogInformation("Connecting to Binance user stream websocket.");

        await socket.ConnectAsync(new Uri(url), cancellationToken);

        _logger.LogInformation("Connected to Binance user stream websocket.");

        await ReceiveLoopAsync(socket, cancellationToken);
    }

    private async Task ReceiveLoopAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
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
                    _logger.LogWarning("Binance user stream websocket closed by remote server.");
                    return;
                }

                message.Append(
                    Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            await _processor.ProcessAsync(message.ToString());
        }
    }
}