using TradingSystem.Domain.Signals;

namespace StrategyService.Strategies;

public interface IBotStrategy
{
    string BotName { get; }

    Task<bool> ProcessSignalAsync(TradingSignal signal, CancellationToken cancellationToken = default);
}