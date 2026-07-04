using System.Text.Json.Serialization;

namespace StrategyService.Services;

public sealed record HealingSnapshotDto
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