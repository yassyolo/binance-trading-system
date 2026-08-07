namespace TradingSystem.EventStore.Models;

public sealed record StoredTradingEvent(long GlobalPosition, EventEnvelope Event);

