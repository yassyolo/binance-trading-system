namespace TradingSystem.HistoricalDatabase;

public interface IHistoricalEventStore
{
    Task AppendAsync(HistoricalEvent historicalEvent, CancellationToken ct);
    Task AppendBatchAsync(IReadOnlyCollection<HistoricalEvent> events, CancellationToken ct);
    Task UpsertTradeAsync(HistoricalTradeSummary trade, CancellationToken ct);
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct);
}

public interface IHistoricalEventSink
{
    Task WriteAsync(HistoricalEvent historicalEvent, CancellationToken ct);
}