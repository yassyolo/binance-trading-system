namespace TradingSystem.Redis.Configuration;

public sealed class RedisPositionStoreOptions
{
    public const string SectionName = "RedisPositionStore";
    public string Prefix { get; set; } = string.Empty;
}