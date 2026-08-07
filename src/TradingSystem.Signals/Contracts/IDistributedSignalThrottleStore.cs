namespace TradingSystem.Signals.Abstractions;

public interface IDistributedSignalThrottleStore
{
    Task<bool> TryAcquireAsync(string botName, string symbol, string side, DateTime signalTimeUtc, TimeSpan minimumInterval, CancellationToken ct);
}
