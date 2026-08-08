namespace TradingSystem.Signals.Configuration;

public sealed class SignalGenerationOptions
{
    public const string SectionName = "SignalGeneration";
    
    public Dictionary<string, BotSignalModeOptions> Bots{ get; set; } = new(StringComparer.OrdinalIgnoreCase);
}