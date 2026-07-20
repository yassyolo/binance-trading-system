using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace TradingSystem.Infrastructure.WebSockets;

public static class WebSocketMessageReader
{
    public static async Task<string?> ReadTextMessageAsync(ClientWebSocket socket,  int bufferSizeBytes,  CancellationToken cancellationToken)
    {
        var buffer  =  ArrayPool<byte>.Shared.Rent(Math.Max(1024,  bufferSizeBytes));
        try
        {
            using var stream  =  new MemoryStream();
            while (true)
            {
                var result  =  await socket.ReceiveAsync(buffer.AsMemory(),  cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                if (result.MessageType == WebSocketMessageType.Text  &&  result.Count > 0)
                    await stream.WriteAsync(buffer.AsMemory(0,  result.Count),  cancellationToken);
                if (result.EndOfMessage) break;
            }
            return stream.Length == 0 ? string.Empty : Encoding.UTF8.GetString(stream.ToArray());
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }
}
