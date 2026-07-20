using System.Text.Json;
using TradingSystem.EventStore;

namespace TradingSystem.ReplayEngine;

public sealed class ReplayEngine(
    IReplayEventSource source, 
    IReplayJobStore store, 
    IEnumerable<IReplayStrategyEvaluator> evaluators)
{
    private readonly IReadOnlyDictionary<string,  IReplayStrategyEvaluator> _evaluators  =  evaluators
        .ToDictionary(x  =>  $"{x.PluginId}:{x.Version}",  StringComparer.OrdinalIgnoreCase);

    public async Task<ReplaySummary> RunAsync(ReplayJob job,  Func<int,  string,  Task>? progress,  CancellationToken cancellationToken)
    {
        var request  =  job.Request;
        var total  =  Math.Max(1,  await source.CountAsync(request,  cancellationToken));
        var state  =  await store.LoadCheckpointAsync(job.ReplayId,  cancellationToken) ?? new ReplayAccumulator();
        var clock  =  new ReplayVirtualClock();
        var cursor  =  job.LastGlobalPosition;
        IReplayStrategyEvaluator? evaluator  =  null;
        if (request.Mode == ReplayMode.StrategyComparison)
        {
            var key  =  $"{request.CandidateStrategyPluginId}:{request.CandidateStrategyVersion}";
            if (!_evaluators.TryGetValue(key,  out evaluator))
                throw new InvalidOperationException($"Replay strategy evaluator '{key}' is not registered.");
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await store.IsCancellationRequestedAsync(job.ReplayId,  cancellationToken))
                throw new OperationCanceledException("Replay cancellation was requested.",  cancellationToken);

            var batch  =  await source.ReadForwardAsync(request,  cursor,  Math.Clamp(request.BatchSize,  1,  1000),  cancellationToken);
            if (batch.Count == 0) break;

            var steps  =  new List<ReplayStepResult>(batch.Count);
            foreach (var item in batch.OrderBy(x  =>  x.GlobalPosition))
            {
                try
                {
                    clock.AdvanceTo(item.Event.OccurredAtUtc);
                    state.Apply(item);
                    ReplayCandidateDecision? candidate  =  null;
                    if (evaluator is not null  &&  item.Event.EventType == TradingEventTypes.StrategyDecisionTaken)
                    {
                        candidate  =  await evaluator.EvaluateAsync(item,  new ReplayContext(job.ReplayId,  clock.UtcNow,  state),  cancellationToken);
                        var recorded  =  ReadDecision(item.Event.PayloadJson);
                        if (candidate is not null  &&  string.Equals(recorded,  candidate.Decision,  StringComparison.OrdinalIgnoreCase)) state.CandidateMatches++;
                        else state.CandidateDifferences++;
                    }
                    steps.Add(new ReplayStepResult(job.ReplayId,  item.GlobalPosition,  item.Event.EventId, 
                        item.Event.EventType,  clock.UtcNow,  true, 
                        JsonSerializer.Serialize(new { replayed  =  true,  candidate },  EventJson.Options),  null));
                }
                catch (Exception exception)
                {
                    state.FailedEvents++;
                    steps.Add(new ReplayStepResult(job.ReplayId,  item.GlobalPosition,  item.Event.EventId, 
                        item.Event.EventType,  clock.UtcNow,  false,  "{}",  exception.Message));
                    if (request.StopOnError)
                    {
                        await store.SaveStepsAsync(steps,  cancellationToken);
                        throw;
                    }
                }
                cursor  =  item.GlobalPosition;
            }

            await store.SaveStepsAsync(steps,  cancellationToken);
            var percent  =  Math.Clamp((int)Math.Round(state.ProcessedEvents * 100d / total),  0,  99);
            await store.SaveCheckpointAsync(job.ReplayId,  cursor,  state,  percent,  $"Replayed through global position {cursor}",  cancellationToken);
            if (progress is not null) await progress(percent,  $"Replayed through global position {cursor}");
        }

        var summary  =  state.ToSummary(job.ReplayId);
        await store.CompleteAsync(job.ReplayId,  summary,  cancellationToken);
        if (progress is not null) await progress(100,  "Completed");
        return summary;
    }

    private static string? ReadDecision(string json)
    {
        try
        {
            using var document  =  JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("decision",  out var value) ? value.GetString() : null;
        }
        catch { return null; }
    }
}
