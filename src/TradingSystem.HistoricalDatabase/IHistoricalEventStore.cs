namespace TradingSystem.HistoricalDatabase;

public interface IHistoricalEventStore
{
    Task AppendAsync(HistoricalEvent historicalEvent, CancellationToken cancellationToken);
    Task AppendBatchAsync(IReadOnlyCollection<HistoricalEvent> events, CancellationToken cancellationToken);
    Task UpsertTradeAsync(HistoricalTradeSummary trade, CancellationToken cancellationToken);
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
}

public interface IHistoricalEventSink
{
    Task WriteAsync(HistoricalEvent historicalEvent, CancellationToken cancellationToken);
}
