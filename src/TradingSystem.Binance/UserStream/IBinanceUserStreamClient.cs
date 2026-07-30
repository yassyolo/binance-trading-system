namespace TradingSystem.Binance.UserStream;
public interface IBinanceUserStreamClient{Task RunAsync(string listenKey, Func<string, CancellationToken, Task> onMessage, Func<CancellationToken, Task>? onConnected, CancellationToken ct);}
