using System.Net.WebSockets;
using TradingSystem.Infrastructure.WebSockets.Configuration;

namespace TradingSystem.Infrastructure.WebSockets;

public static class ClientWebSocketFactory
{
    public static ClientWebSocket Create(WebSocketOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        var socket  =  new ClientWebSocket();
       
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Math.Max(1, options.KeepAliveIntervalSeconds));
        
        return socket;
    }
}
