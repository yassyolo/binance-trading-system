namespace TradingSystem.StrategyPlugins;

public sealed record StrategyPluginDescriptor(
    string PluginId, 
    string DisplayName, 
    string Version, 
    string StrategyName, 
    string AssemblyName, 
    bool IsBuiltIn, 
    IReadOnlyCollection<string> SupportedSymbols, 
    string? Description  =  null);