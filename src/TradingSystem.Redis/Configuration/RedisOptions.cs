namespace TradingSystem.Redis.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public bool AbortOnConnectFail { get; set; } = false;
    public string KeyPrefix { get; set; } = string.Empty;
    public int ConnectRetry { get; set; } = 5;
    public int ConnectTimeoutMilliseconds { get; set; } = 5_000;
    public int SyncTimeoutMilliseconds { get; set; } = 5_000;
    public int AsyncTimeoutMilliseconds { get; set; } = 5_000;
    public int KeepAliveSeconds { get; set; } = 30;
}
