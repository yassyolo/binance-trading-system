using System.Text.Json;
using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Accumulator;
using TradingSystem.ReplayEngine.Clock;
using TradingSystem.ReplayEngine.Contracts;
using TradingSystem.ReplayEngine.Evaluator;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Models.Enums;
using TradingSystem.ReplayEngine.Store;

namespace TradingSystem.ReplayEngine.Engine;

public sealed class ReplayEngine(
    IReplayEventSource source,
    IReplayJobStore store,
    IEnumerable<IReplayStrategyEvaluator> evaluators)
{
    private readonly IReadOnlyDictionary<string, IReplayStrategyEvaluator> _evaluators 
        = evaluators.ToDictionary(x => $"{x.PluginId}:{x.Version}", StringComparer.OrdinalIgnoreCase);

    public async Task<ReplaySummary> RunAsync(ReplayJob job, Func<int, string, Task>? progress, CancellationToken ct)
    {
        var request = Validate(job.Request);
        var total = Math.Max(1L, await source.CountAsync(request, ct));
        var state = await store.LoadCheckpointAsync(job.ReplayId, ct) ?? new ReplayAccumulator();
        var clock = new ReplayVirtualClock();
        var cursor = Math.Max(job.LastGlobalPosition, 0);
        var evaluator = ResolveEvaluator(request);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            
            if (await store.IsCancellationRequestedAsync(job.ReplayId, ct))
            {
                await store.MarkCancelledAsync(job.ReplayId, ct);
                
                throw new OperationCanceledException("Replay cancellation was requested.", ct);
            }

            var batch = await source.ReadForwardAsync(request, cursor, Math.Clamp(request.BatchSize, 1, 1000), ct);
            if (batch.Count == 0)
                break;

            var orderedBatch = batch.OrderBy(item => item.GlobalPosition).ToArray();
            EnsureStrictlyForward(cursor, orderedBatch);
            var steps = new List<ReplayStepResult>(orderedBatch.Length);

            foreach (var item in orderedBatch)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    clock.AdvanceTo(item.Event.OccurredAtUtc);
                    state.Apply(item);

                    ReplayCandidateDecision? candidate = null;
                    if (evaluator is not null && item.Event.EventType == TradingEventTypes.StrategyDecisionTaken)
                    {
                        candidate = await evaluator.EvaluateAsync(item, new ReplayContext(job.ReplayId, clock.UtcNow, state), ct);

                        var recordedDecision = ReadDecision(item.Event.PayloadJson);
                        
                        if (candidate is not null && string.Equals(recordedDecision, candidate.Decision, StringComparison.OrdinalIgnoreCase))
                            state.CandidateMatches++;
                        else
                            state.CandidateDifferences++;
                    }

                    steps.Add(new ReplayStepResult(
                        job.ReplayId,
                        item.GlobalPosition,
                        item.Event.EventId,
                        item.Event.EventType,
                        clock.UtcNow,
                        true,
                        JsonSerializer.Serialize(new { replayed = true, candidate }, EventJson.Options),
                        null));
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    state.FailedEvents++;
                    steps.Add(new ReplayStepResult(
                        job.ReplayId,
                        item.GlobalPosition,
                        item.Event.EventId,
                        item.Event.EventType,
                        clock.UtcNow,
                        false,
                        "{}",
                        exception.Message));

                    if (request.StopOnError)
                    {
                        await store.SaveStepsAsync(steps, ct);
                        throw;
                    }
                }

                cursor = item.GlobalPosition;
            }

            await store.SaveStepsAsync(steps, ct);
            var percent = Math.Clamp((int)Math.Round(state.ProcessedEvents * 100d / total), 0, 99);
            var stage = $"Replayed through global position {cursor}";
            
            await store.SaveCheckpointAsync(job.ReplayId, cursor, state, percent, stage, ct);
            
            if (progress is not null)
                await progress(percent, stage);
        }

        var summary = state.ToSummary(job.ReplayId);
       
        await store.CompleteAsync(job.ReplayId, summary, ct);
        
        if (progress is not null)
            await progress(100, "Completed");
        
        return summary;
    }

    private IReplayStrategyEvaluator? ResolveEvaluator(CreateReplayRequest request)
    {
        if (request.Mode != ReplayMode.StrategyComparison)
            return null;

        var key = $"{request.CandidateStrategyPluginId}:{request.CandidateStrategyVersion}";
        if (!_evaluators.TryGetValue(key, out var evaluator))
            throw new InvalidOperationException($"Replay strategy evaluator '{key}' is not registered.");
        
        return evaluator;
    }

    private static CreateReplayRequest Validate(CreateReplayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Replay name is required.", nameof(request));
        if (request.BatchSize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(request), "Replay batch size must be between 1 and 1000.");
        if (request.FromGlobalPosition.HasValue && request.ToGlobalPosition.HasValue &&
            request.ToGlobalPosition < request.FromGlobalPosition)
            throw new ArgumentException("ToGlobalPosition cannot be before FromGlobalPosition.", nameof(request));
        if (request.FromUtc.HasValue && request.ToUtc.HasValue && request.ToUtc <= request.FromUtc)
            throw new ArgumentException("ToUtc must be after FromUtc.", nameof(request));
        if (request.Mode == ReplayMode.StrategyComparison &&
            (string.IsNullOrWhiteSpace(request.CandidateStrategyPluginId) ||
             string.IsNullOrWhiteSpace(request.CandidateStrategyVersion)))
            throw new ArgumentException("Candidate strategy plugin id and version are required for strategy comparison.", nameof(request));
        return request;
    }

    private static void EnsureStrictlyForward(long cursor, IReadOnlyList<StoredTradingEvent> batch)
    {
        var previous = cursor;
        foreach (var item in batch)
        {
            if (item.GlobalPosition <= previous)
                throw new InvalidOperationException($"Replay source returned non-forward global position {item.GlobalPosition} after {previous}.");
           
            previous = item.GlobalPosition;
        }
    }

    private static string? ReadDecision(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("decision", out var value)
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
