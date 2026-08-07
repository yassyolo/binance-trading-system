using TradingSystem.Reconciliation.Models;

namespace TradingSystem.Reconciliation.Contracts;

public interface IExchangeStateProvider
{
    Task<ExchangeStateSnapshot> GetAsync(string symbol, CancellationToken ct);
}
