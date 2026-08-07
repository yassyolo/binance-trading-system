using TradingSystem.Operations.Models;

namespace TradingSystem.Operations.Contracts;

public interface IAlertCandidateSource
{
    string SourcePrefix { get; }

    Task<IReadOnlyCollection<AlertCandidate>> LoadAsync(CancellationToken ct);
}
