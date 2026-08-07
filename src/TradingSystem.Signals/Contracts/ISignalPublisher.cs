using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Contracts;

public interface ISignalPublisher 
{ 
	Task PublishAsync(GeneratedTradingSignal signal, CancellationToken ct); 
}
