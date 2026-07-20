namespace TradingSystem.Binance.Configuration;

public sealed class BinanceFuturesOptions
{
    public const string SectionName  =  "BinanceFutures";
    public string BaseUrl {  get;  set; }  =  "https://fapi.binance.com";
    public string ApiKey {  get;  set; }  =  string.Empty;
    public string SecretKey {  get;  set; }  =  string.Empty;
    public int ReceiveWindow {  get;  set; }  =  5000;
    public TimeSpan ExchangeInfoCacheDuration {  get;  set; }  =  TimeSpan.FromHours(1);
}
