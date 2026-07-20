using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Configuration;
public sealed class SignalGenerationOptions{public const string SectionName = "SignalGeneration";public Dictionary<string, BotSignalModeOptions> Bots{ get; set;} = new(StringComparer.OrdinalIgnoreCase);}
public sealed class BotSignalModeOptions{public bool Enabled{ get; set;} = true;public SignalGenerationMode Mode{ get; set;} = SignalGenerationMode.TradingViewOnly;public string Symbol{ get; set;} = string.Empty;public string Interval{ get; set;} = "1m";public int MinimumSecondsBetweenGeneratedSignals{ get; set;} = 60;}
