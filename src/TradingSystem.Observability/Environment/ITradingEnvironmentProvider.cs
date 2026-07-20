namespace TradingSystem.Observability.Environment;
public interface ITradingEnvironmentProvider { string EnvironmentName {  get;  } }
public sealed class TradingEnvironmentProvider:ITradingEnvironmentProvider
{ 
    public string EnvironmentName => System.Environment.GetEnvironmentVariable("TRADING_ENVIRONMENT")?.Trim() is {Length:>0} x?x:"Demo"; 
}
