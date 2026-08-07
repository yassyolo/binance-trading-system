namespace TradingSystem.JobOrchestration.Contracts;

public interface IHistoricalSignalStore
{
    Task<IReadOnlyList<Backtesting.Bots.Common.HistoricalBotSignal>> LoadAsync(string botName, string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
