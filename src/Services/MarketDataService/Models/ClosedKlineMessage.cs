using System.Text.Json.Serialization;

namespace MarketDataService.Models;

public sealed record ClosedKlineMessage(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("time")] long Time,
    [property: JsonPropertyName("open")] string? Open,
    [property: JsonPropertyName("high")] string? High,
    [property: JsonPropertyName("low")] string? Low,
    [property: JsonPropertyName("close")] string? Close,
    [property: JsonPropertyName("volume")] string? Volume,
    [property: JsonPropertyName("close_time")] long CloseTime,
    [property: JsonPropertyName("interval")] string Interval);