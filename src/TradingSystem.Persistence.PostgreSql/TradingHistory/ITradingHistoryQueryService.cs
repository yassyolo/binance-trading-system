using TradingSystem.Persistence.PostgreSql.TradingHistory.Models;

namespace TradingSystem.Persistence.PostgreSql.TradingHistory;

public interface ITradingHistoryQueryService
{
    Task<IReadOnlyCollection<SignalListItem>> GetRecentSignalsAsync(string? botName, string? environment, int limit, CancellationToken ct);
    
    Task<TradingSummary> GetSummaryAsync(string botName, string environment, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    
    Task<IReadOnlyCollection<BlockReasonSummary>> GetBlockReasonsAsync(string botName, string environment, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}