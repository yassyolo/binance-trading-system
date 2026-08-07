using System.Text.Json.Serialization;

namespace TradingSystem.ReplayEngine.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReplayMode 
{ 
    Timeline, 
    Projection, 
    StrategyComparison 
}
