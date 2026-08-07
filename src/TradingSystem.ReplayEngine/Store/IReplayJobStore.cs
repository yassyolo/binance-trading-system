using TradingSystem.ReplayEngine.Accumulator;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Models.Enums;

namespace TradingSystem.ReplayEngine.Store;

public interface IReplayJobStore
{
    Task<Guid> EnqueueAsync(CreateReplayRequest request, string requestedBy, CancellationToken ct);
    
    Task<IReadOnlyList<ReplayJob>> ClaimAsync(string workerId, int take, TimeSpan staleAfter, CancellationToken ct);
    
    Task<ReplayJob?> GetAsync(Guid replayId, CancellationToken ct);
    
    Task<ReplaySummary?> GetSummaryAsync(Guid replayId, CancellationToken ct);
    
    Task<IReadOnlyList<ReplayStepResult>> GetStepsAsync(Guid replayId, long afterGlobalPosition, int take, CancellationToken ct);
    
    Task<IReadOnlyList<ReplayJob>> QueryAsync(ReplayJobStatus? status, int skip, int take, CancellationToken ct);
    
    Task<ReplayAccumulator?> LoadCheckpointAsync(Guid replayId, CancellationToken ct);
    
    Task SaveCheckpointAsync(Guid replayId, long globalPosition, ReplayAccumulator accumulator, int progressPercent, string progressStage, CancellationToken ct);
    
    Task SaveStepsAsync(IReadOnlyCollection<ReplayStepResult> steps, CancellationToken ct);
    
    Task CompleteAsync(Guid replayId, ReplaySummary summary, CancellationToken ct);
   
    Task FailAsync(Guid replayId, string error, CancellationToken ct);
    
    Task CancelAsync(Guid replayId, string actor, CancellationToken ct);
    
    Task MarkCancelledAsync(Guid replayId, CancellationToken ct);
    
    Task<bool> IsCancellationRequestedAsync(Guid replayId, CancellationToken ct);
}
