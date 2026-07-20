namespace TradingSystem.Persistence.PostgreSql.TradingHistory;
public interface ITradingHistoryQueryService
{Task<IReadOnlyCollection<SignalListItem>> GetRecentSignalsAsync(string? botName, string? environment, int limit, CancellationToken ct);Task<TradingSummary> GetSummaryAsync(string botName, string environment, DateTime fromUtc, DateTime toUtc, CancellationToken ct);Task<IReadOnlyCollection<BlockReasonSummary>> GetBlockReasonsAsync(string botName, string environment, DateTime fromUtc, DateTime toUtc, CancellationToken ct);}
public sealed record SignalListItem(string SignalId, string BotName, string StrategyVersion, string Symbol, string Side, string Source, string Environment, DateTime SignalTimeUtc, decimal? ReferencePrice, string? Reason);
public sealed record TradingSummary(long Signals, long OpenDecisions, long BlockedDecisions, long Positions, long ClosedPositions, decimal RealizedPnl);
public sealed record BlockReasonSummary(string Reason, long Count);
