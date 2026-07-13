using System.Text.Json.Serialization;

namespace TradingSystem.Contracts.Signals;

public sealed record TradingSignalMessage(
    [property: JsonPropertyName("signal_id")] string SignalId,
    [property: JsonPropertyName("bot_name")] string BotName,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("generated_at_utc")] DateTime? GeneratedAtUtc);
