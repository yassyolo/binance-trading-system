using System.Text.Json;
using System.Text.Json.Serialization;

namespace UserStreamService.Models;

public sealed record HealingSnapshot
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "healing_snapshot";

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = "reconnected";

    [JsonPropertyName("hub_ts")]
    public string HubTimestamp { get; init; } = string.Empty;

    [JsonPropertyName("snapshot_ts")]
    public string SnapshotTimestamp { get; init; } = string.Empty;

    [JsonPropertyName("downtime_seconds")]
    public double DowntimeSeconds { get; init; }

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("normal_orders_count")]
    public int NormalOrdersCount { get; init; }

    [JsonPropertyName("algo_orders_count")]
    public int AlgoOrdersCount { get; init; }

    [JsonPropertyName("total_orders_count")]
    public int TotalOrdersCount { get; init; }

    [JsonPropertyName("active_client_ids")]
    public List<string> ActiveClientIds { get; init; } = [];

    [JsonPropertyName("normal_orders")]
    public JsonElement[] NormalOrders { get; init; } = [];

    [JsonPropertyName("algo_orders")]
    public JsonElement[] AlgoOrders { get; init; } = [];
}