using TradingSystem.EventStore;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Models;

namespace TradingSystem.ReplayEngine.Evaluator;

public interface IReplayStrategyEvaluator
{
    string PluginId { get; }
    
    string Version { get; }
    
    ValueTask<ReplayCandidateDecision?> EvaluateAsync(StoredTradingEvent sourceEvent, ReplayContext context, CancellationToken ct);
}
