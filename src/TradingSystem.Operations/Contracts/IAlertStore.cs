using TradingSystem.Operations.Models;

namespace TradingSystem.Operations.Contracts;

public interface IAlertStore
{
    Task UpsertActiveAsync(AlertCandidate alert, CancellationToken ct);
    Task ResolveMissingAsync(string sourcePrefix, IReadOnlyCollection<string> activeKeys, CancellationToken ct);
}
