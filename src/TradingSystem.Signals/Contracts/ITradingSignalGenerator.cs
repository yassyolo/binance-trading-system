using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Abstractions;

public interface ITradingSignalGenerator
{
	string BotName { get; } 
	
	string StrategyVersion { get; } 
	
	IReadOnlyCollection<string> SupportedSymbols { get; }
	
	ValueTask<GeneratedTradingSignal?> GenerateAsync(MarketIndicatorSnapshot snapshot, CancellationToken ct);
}
