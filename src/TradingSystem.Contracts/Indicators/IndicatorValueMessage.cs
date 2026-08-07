using System.Text.Json.Serialization;

namespace TradingSystem.Contracts.Indicators;

public sealed record IndicatorValueMessage
{
    [JsonPropertyName("value")]
    public required decimal Value { get; init; }

    [JsonPropertyName("previous_value")]
    public decimal? PreviousValue { get; init; }

    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
