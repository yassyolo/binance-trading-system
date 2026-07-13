using System.Text.Json.Serialization;

namespace AlligatorIndicatorService.Models;

public sealed record AlligatorMaPayload
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "alligator_ma";

    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("timeframe")]
    public string Timeframe { get; init; } = string.Empty;

    [JsonPropertyName("candle_close_time")]
    public long CandleCloseTime { get; init; }

    [JsonPropertyName("published_at")]
    public long PublishedAt { get; init; }

    [JsonPropertyName("indicators")]
    public AlligatorIndicators Indicators { get; init; } = new();
}

public sealed record AlligatorIndicators
{
    [JsonPropertyName("alligator_jaw")]
    public IndicatorValue AlligatorJaw { get; init; } = new();

    [JsonPropertyName("alligator_teeth")]
    public IndicatorValue AlligatorTeeth { get; init; } = new();

    [JsonPropertyName("alligator_lips")]
    public IndicatorValue AlligatorLips { get; init; } = new();

    [JsonPropertyName("sma200")]
    public IndicatorValue Sma200 { get; init; } = new();
}

public sealed record IndicatorValue
{
    [JsonPropertyName("value")]
    public decimal Value { get; init; }
}
