using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Execution.Contracts;

public interface ITradingSignalHandler
{
    Task<bool> HandleAsync(TradeSignal signal, CancellationToken ct);
}
