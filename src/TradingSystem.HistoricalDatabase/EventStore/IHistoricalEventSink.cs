using TradingSystem.HistoricalDatabase.Models;

namespace TradingSystem.HistoricalDatabase.EventStore;

public interface IHistoricalEventSink
{
    Task WriteAsync(HistoricalEvent historicalEvent, CancellationToken ct);
}
