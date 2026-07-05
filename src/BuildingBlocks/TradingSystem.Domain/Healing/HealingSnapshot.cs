using System.Text.Json.Serialization;

namespace TradingSystem.Domain.Healing;

public sealed record HealingSnapshot
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("downtime_seconds")]
    public decimal DowntimeSeconds { get; init; }

    [JsonPropertyName("active_client_ids")]
    public List<string> ActiveClientIds { get; init; } = [];
}