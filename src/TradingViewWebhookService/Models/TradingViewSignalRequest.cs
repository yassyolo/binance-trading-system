using System.Text.Json.Serialization;

namespace TradingViewWebhookService.Models;

public sealed record TradingViewSignalRequest(
    [property: JsonPropertyName("secret")] string? Secret,
    [property: JsonPropertyName("action")] string? Action,
    [property: JsonPropertyName("symbol")] string? Symbol = null,
    [property: JsonPropertyName("signal_id")] string? SignalId = null,
    [property: JsonPropertyName("generated_at_utc")] DateTime? GeneratedAtUtc = null);
