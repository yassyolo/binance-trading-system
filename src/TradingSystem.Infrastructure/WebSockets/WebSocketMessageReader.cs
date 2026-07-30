using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace TradingSystem.Infrastructure.WebSockets;

public static class WebSocketMessageReader
{
    private const int MaximumMessageSizeBytes = 1024 * 1024;

    public static async Task<string?> ReadTextMessageAsync(
        ClientWebSocket socket,
        int bufferSizeBytes,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(socket);
        if (bufferSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSizeBytes));

        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(1024, bufferSizeBytes));
        try
        {
            using var stream = new MemoryStream();
            while (true)
            {
                var result = await socket.ReceiveAsync(buffer.AsMemory(), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                if (result.MessageType != WebSocketMessageType.Text)
                    throw new InvalidDataException($"Unsupported WebSocket message type '{result.MessageType}'.");

                if (result.Count > 0)
                {
                    if (stream.Length + result.Count > MaximumMessageSizeBytes)
                        throw new InvalidDataException($"WebSocket message exceeds {MaximumMessageSizeBytes} bytes.");

                    await stream.WriteAsync(buffer.AsMemory(0, result.Count), ct);
                }

                if (result.EndOfMessage)
                    break;
            }

            return stream.Length == 0 ? string.Empty : Encoding.UTF8.GetString(stream.GetBuffer(), 0, checked((int)stream.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
