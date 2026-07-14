namespace TradingSystem.Infrastructure.WebSockets;

public sealed class WebSocketOptions
{
    public int KeepAliveIntervalSeconds { get; set; } = 30;
    public int ReceiveBufferSizeBytes { get; set; } = 32 * 1024;
}
