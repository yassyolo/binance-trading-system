using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Execution;

public interface ITradingSignalHandler
{
    Task<bool> HandleAsync(
        TradeSignal signal,
        CancellationToken cancellationToken);
}
