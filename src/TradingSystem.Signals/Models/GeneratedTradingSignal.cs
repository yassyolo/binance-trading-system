namespace TradingSystem.Signals.Models;

public sealed record GeneratedTradingSignal(
    string SignalId, 
    string BotName, 
    string StrategyVersion, 
    string Symbol, 
    string Action, 
    string Source, 
    DateTime GeneratedAtUtc, 
    DateTime CandleOpenTimeUtc, 
    string Interval, 
    decimal Price, 
    string Reason, 
    IReadOnlyDictionary<string, object?> Metadata);
