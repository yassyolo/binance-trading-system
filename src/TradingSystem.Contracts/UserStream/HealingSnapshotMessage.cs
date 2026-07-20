using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradingSystem.Contracts.UserStream;

public sealed record HealingSnapshotMessage
{
    [JsonPropertyName("type")] public string Type {  get;  init;  }  =  "healing_snapshot";
    [JsonPropertyName("reason")] public string Reason {  get;  init;  }  =  "reconnected";
    [JsonPropertyName("hub_ts_utc")] public required DateTime HubTimestampUtc {  get;  init;  }
    [JsonPropertyName("snapshot_ts_utc")] public required DateTime SnapshotTimestampUtc {  get;  init;  }
    [JsonPropertyName("downtime_seconds")] public required double DowntimeSeconds {  get;  init;  }
    [JsonPropertyName("symbol")] public required string Symbol {  get;  init;  }
    [JsonPropertyName("normal_orders_count")] public required int NormalOrdersCount {  get;  init;  }
    [JsonPropertyName("algo_orders_count")] public required int AlgoOrdersCount {  get;  init;  }
    [JsonPropertyName("total_orders_count")] public required int TotalOrdersCount {  get;  init;  }
    [JsonPropertyName("active_client_ids")] public required IReadOnlyCollection<string> ActiveClientIds {  get;  init;  }
    [JsonPropertyName("normal_orders")] public required JsonElement[] NormalOrders {  get;  init;  }
    [JsonPropertyName("algo_orders")] public required JsonElement[] AlgoOrders {  get;  init;  }
}
