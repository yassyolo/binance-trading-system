using System.Text.Json;
using System.Text.Json.Serialization;

namespace UserStreamService.Models;

public sealed record UserStreamEnvelope
{
    [JsonPropertyName("hub_ts")]
    public string HubTimestamp { get; init; } = string.Empty;

    [JsonPropertyName("hub_seq")]
    public long HubSequence { get; init; }

    [JsonPropertyName("binance")]
    public JsonElement Binance { get; init; }
}