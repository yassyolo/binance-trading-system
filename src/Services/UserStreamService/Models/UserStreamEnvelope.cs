using System.Text.Json;
using System.Text.Json.Serialization;

namespace UserStreamService.Models;

public sealed record UserStreamEnvelope
{
    [JsonPropertyName("hub_ts")]
    public required string HubTimestamp { get; init; }

    [JsonPropertyName("hub_seq")]
    public required long HubSequence { get; init; }

    [JsonPropertyName("binance")]
    public required JsonElement Binance { get; init; }
}
