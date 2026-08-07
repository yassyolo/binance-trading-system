namespace TradingSystem.Binance.UserStream.Contracts;

public interface IBinanceListenKeyClient
{
	Task<string>CreateAsync(CancellationToken ct);
	
	Task KeepAliveAsync(string listenKey, CancellationToken ct);
}
