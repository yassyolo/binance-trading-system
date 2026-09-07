using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;

namespace TradingSystem.EventStore.TradingTimeline;

public sealed class TradingEventStoreReader(
    ITradingEventStore tradingEventStore) 
    : ITradingEventStoreReader
{
    public async Task<IReadOnlyList<TradingEventStoreReadItem>> ReadAsync(EventStoreQuery query, CancellationToken ct)
         => (await tradingEventStore.ReadAsync(query, ct))
                .Select(x => new TradingEventStoreReadItem(
                    x.GlobalPosition, 
                    x.Event.EventId, 
                    x.Event.OccurredAtUtc, 
                    x.Event.EventType,
                    x.Event.AggregateType, 
                    x.Event.AggregateId, 
                    x.Event.BotName, 
                    x.Event.Symbol,
                    x.Event.PositionId, 
                    x.Event.SignalId, 
                    x.Event.CorrelationId,
                    x.Event.PayloadJson, 
                    x.Event.MetadataJson))
                .ToArray();
    }
