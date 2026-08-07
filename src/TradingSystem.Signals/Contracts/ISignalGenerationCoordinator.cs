using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Contracts;

public interface ISignalGenerationCoordinator
{
    Task ProcessAsync(MarketIndicatorSnapshot snapshot, CancellationToken ct);
}
