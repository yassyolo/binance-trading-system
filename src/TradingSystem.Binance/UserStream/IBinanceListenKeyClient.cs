namespace TradingSystem.Binance.UserStream;
public interface IBinanceListenKeyClient{Task<string>CreateAsync(CancellationToken ct);Task KeepAliveAsync(string listenKey, CancellationToken ct);}
