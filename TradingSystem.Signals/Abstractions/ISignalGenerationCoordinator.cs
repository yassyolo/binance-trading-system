using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Abstractions;

public interface ISignalGenerationCoordinator
{
    Task ProcessAsync(MarketIndicatorSnapshot snapshot, CancellationToken cancellationToken);
}
