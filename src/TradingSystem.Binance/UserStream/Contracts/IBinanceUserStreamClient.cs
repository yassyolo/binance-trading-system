namespace TradingSystem.Binance.UserStream.Contracts;

public interface IBinanceUserStreamClient
{
	Task RunAsync(string listenKey, Func<string, CancellationToken, Task> onMessage, Func<CancellationToken, Task>? onConnected, CancellationToken ct);
}
