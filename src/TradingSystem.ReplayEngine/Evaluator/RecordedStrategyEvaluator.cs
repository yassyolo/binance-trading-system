using System.Text.Json;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Models;

namespace TradingSystem.ReplayEngine.Evaluator;

public sealed class RecordedStrategyEvaluator : IReplayStrategyEvaluator
{
    public string PluginId  => "recorded-strategy";
    public string Version  => "1.0.0";

    public ValueTask<ReplayCandidateDecision?> EvaluateAsync(StoredTradingEvent sourceEvent, ReplayContext context, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(sourceEvent.Event.PayloadJson);
       
        var decision = document.RootElement.TryGetProperty("decision",  out var d) 
            ? d.GetString() ?? "Unknown" : "Unknown";
        
        var reason = document.RootElement.TryGetProperty("reason",  out var r) 
            ? r.GetString() ?? "Recorded decision" : "Recorded decision";
        
        return ValueTask.FromResult<ReplayCandidateDecision?>(new(decision,  reason,  "{}"));
    }
}
