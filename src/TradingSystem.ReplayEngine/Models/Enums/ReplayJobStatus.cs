using System.Text.Json.Serialization;

namespace TradingSystem.ReplayEngine.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReplayJobStatus 
{
    Pending, 
    Processing, 
    Paused,
    Completed, 
    Failed, 
    Cancelled
}
