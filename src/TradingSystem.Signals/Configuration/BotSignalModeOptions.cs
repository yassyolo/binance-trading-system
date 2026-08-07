using TradingSystem.Signals.Models.Enums;

namespace TradingSystem.Signals.Configuration;

public sealed class BotSignalModeOptions
{
    public bool Enabled { get; set; } = true;

    public SignalGenerationMode Mode { get; set; } = SignalGenerationMode.TradingViewOnly;

    public string Symbol { get; set; } = string.Empty;

    public string Interval { get; set; } = "1m";

    public int MinimumSecondsBetweenGeneratedSignals { get; set; } = 60;
}

