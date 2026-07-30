using TradingSystem.Signals.Models;
namespace TradingSystem.Signals.Abstractions;
public interface ISignalPublisher { Task PublishAsync(GeneratedTradingSignal signal, CancellationToken ct); }
