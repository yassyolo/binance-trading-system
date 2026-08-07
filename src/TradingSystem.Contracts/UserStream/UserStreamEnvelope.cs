using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradingSystem.Contracts.UserStream;

public sealed record UserStreamEnvelope
{
    [JsonPropertyName("hub_ts_utc")] 
    public required DateTime HubTimestampUtc { get; init; }
    
    [JsonPropertyName("hub_seq")] 
    public required long HubSequence { get; init; }
    
    [JsonPropertyName("binance")] 
    public required JsonElement Binance { get; init; }
}
