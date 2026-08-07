using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Strategies.Models;

public sealed record StrategyMetadata(
    string Name, 
    string Version, 
    PositionMode PositionMode, 
    IReadOnlyCollection<string> SupportedSymbols, 
    string? PluginId  =  null, 
    string? DisplayName  =  null, 
    string? Description  =  null)
{
    public string EffectivePluginId  =>  string.IsNullOrWhiteSpace(PluginId) ? Name : PluginId;
}
