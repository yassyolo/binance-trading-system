namespace TradingSystem.Redis.Configuration;

public sealed class RedisOptions
{
    public const string SectionName  =  "Redis";
    public string ConnectionString {  get;  set; }  =  "localhost:6379";
    public bool AbortOnConnectFail {  get;  set; }  =  false;
    public string KeyPrefix {  get;  set; }  =  string.Empty;
}
