namespace TradingSystem.Binance.UserStream;
public interface IBinanceListenKeyClient{Task<string>CreateAsync(CancellationToken cancellationToken);Task KeepAliveAsync(string listenKey, CancellationToken cancellationToken);}
