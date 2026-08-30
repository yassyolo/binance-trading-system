using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;

namespace TradingSystem.EventStore.TradingTimeline;

public sealed class TradingTimelineReader(
    ITradingEventStore store) 
    : ITradingTimelineReader
{
    public async Task<IReadOnlyList<TradingTimelineItem>> ReadAsync(EventStoreQuery query, CancellationToken ct)
         => (await store.ReadAsync(query, ct))
                .Select(x => new TradingTimelineItem(
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
