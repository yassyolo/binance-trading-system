using TradingSystem.Dashboard.Contracts;
namespace TradingSystem.JobOrchestration;
public enum DashboardJobStatus { Pending,  Processing,  Completed,  Failed,  Cancelled }
public sealed record DashboardJob(Guid JobId, string Type, DashboardJobStatus Status, string RequestedBy, string RequestJson, int AttemptCount, DateTime CreatedAtUtc, DateTime? StartedAtUtc);
public interface IDashboardJobQueue
{
 Task<IReadOnlyList<DashboardJob>> ClaimAsync(string type, string workerId, int batchSize, TimeSpan processingTimeout, CancellationToken ct);
 Task ReportProgressAsync(Guid jobId, int percent, string stage, CancellationToken ct);
 Task CompleteAsync(Guid jobId, Guid runId, CancellationToken ct);
 Task FailAsync(Guid jobId, string error, int maxAttempts, TimeSpan retryDelay, CancellationToken ct);
}
public sealed record HistoricalDataGap(string Symbol, string Interval, DateTime FromUtc, DateTime ToUtc, int MissingCandles);
public interface IHistoricalMarketDataStore
{
 Task<IReadOnlyList<TradingSystem.Domain.MarketData.MarketCandle>> LoadCandlesAsync(string symbol, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
 Task UpsertCandlesAsync(IReadOnlyCollection<TradingSystem.Domain.MarketData.MarketCandle> candles, CancellationToken ct);
 Task ReplaceGapsAsync(string symbol, string interval, IReadOnlyCollection<HistoricalDataGap> gaps, CancellationToken ct);
 Task<DateTime?> GetLatestOpenTimeAsync(string symbol, string interval, CancellationToken ct);
 Task<bool> HasGapsAsync(string symbol, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
public interface IHistoricalSignalStore
{
 Task<IReadOnlyList<TradingSystem.Backtesting.Bots.Common.HistoricalBotSignal>> LoadAsync(string botName, string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
