using TradingSystem.Backtesting.Bots.Models;

namespace TradingSystem.JobOrchestration.Contracts;

public interface IHistoricalSignalStore
{
    Task<IReadOnlyList<HistoricalBotSignal>> LoadAsync(string botName, string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
