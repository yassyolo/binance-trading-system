using System.Text.Json.Serialization;
namespace TradingSystem.Contracts.Indicators;
public sealed record IndicatorSnapshotMessage
{
    [JsonPropertyName("type")] public required string Type {  get;  init;  }
    [JsonPropertyName("symbol")] public required string Symbol {  get;  init;  }
    [JsonPropertyName("timeframe")] public required string Timeframe {  get;  init;  }
    [JsonPropertyName("candle_open_time")] public long CandleOpenTime {  get;  init;  }
    [JsonPropertyName("candle_close_time")] public long CandleCloseTime {  get;  init;  }
    [JsonPropertyName("published_at")] public long PublishedAt {  get;  init;  }
    [JsonPropertyName("indicators")] public required IReadOnlyDictionary<string,  IndicatorValueMessage> Indicators {  get;  init;  }
}
public sealed record IndicatorValueMessage
{
    [JsonPropertyName("value")] public required decimal Value {  get;  init;  }
    [JsonPropertyName("previous_value")] public decimal? PreviousValue {  get;  init;  }
    [JsonPropertyName("metadata")] public IReadOnlyDictionary<string, string>? Metadata {  get;  init;  }
}
