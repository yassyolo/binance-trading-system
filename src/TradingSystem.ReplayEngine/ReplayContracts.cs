using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingSystem.EventStore;

namespace TradingSystem.ReplayEngine;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReplayJobStatus { Pending, Processing, Paused, Completed, Failed, Cancelled }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReplayMode { Timeline, Projection, StrategyComparison }

public sealed record CreateReplayRequest(
	string Name,
	ReplayMode Mode,
	long? FromGlobalPosition = null,
	long? ToGlobalPosition = null,
	DateTime? FromUtc = null,
	DateTime? ToUtc = null,
	string? BotName = null,
	string? Symbol = null,
	string? CorrelationId = null,
	string? CandidateStrategyPluginId = null,
	string? CandidateStrategyVersion = null,
	int BatchSize = 250,
	bool StopOnError = true);

public sealed record ReplayJob(
	Guid ReplayId,
	string Name,
	ReplayMode Mode,
	ReplayJobStatus Status,
	string RequestedBy,
	CreateReplayRequest Request,
	long LastGlobalPosition,
	long ProcessedEvents,
	long FailedEvents,
	int ProgressPercent,
	string? ProgressStage,
	string? Error,
	DateTime CreatedAtUtc,
	DateTime? StartedAtUtc,
	DateTime? CompletedAtUtc,
	string? DeterministicHash);

public sealed record ReplayStepResult(
	Guid ReplayId,
	long GlobalPosition,
	Guid SourceEventId,
	string EventType,
	DateTime VirtualTimeUtc,
	bool Succeeded,
	string ResultJson,
	string? Error);

public sealed record ReplaySummary(
	Guid ReplayId,
	long ProcessedEvents,
	long FailedEvents,
	long Signals,
	long StrategyDecisions,
	long RiskAllowed,
	long RiskBlocked,
	long ExecutionsCompleted,
	long ExecutionsFailed,
	long PositionsOpened,
	long PositionsClosed,
	long CandidateMatches,
	long CandidateDifferences,
	string DeterministicHash);

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

public interface IReplayEventSource
{
	Task<IReadOnlyList<StoredTradingEvent>> ReadForwardAsync(CreateReplayRequest request, long afterGlobalPosition, int take, CancellationToken ct);
	Task<long> CountAsync(CreateReplayRequest request, CancellationToken ct);
}

public interface IReplayStrategyEvaluator
{
	string PluginId { get; }
	string Version { get; }
	ValueTask<ReplayCandidateDecision?> EvaluateAsync(StoredTradingEvent sourceEvent, ReplayContext context, CancellationToken ct);
}

public sealed record ReplayCandidateDecision(string Decision, string Reason, string DataJson);
public sealed record ReplayContext(Guid ReplayId, DateTime VirtualTimeUtc, ReplayAccumulator State);

public sealed class ReplayAccumulator
{
	public long ProcessedEvents { get; set; }
	public long FailedEvents { get; set; }
	public long Signals { get; set; }
	public long StrategyDecisions { get; set; }
	public long RiskAllowed { get; set; }
	public long RiskBlocked { get; set; }
	public long ExecutionsCompleted { get; set; }
	public long ExecutionsFailed { get; set; }
	public long PositionsOpened { get; set; }
	public long PositionsClosed { get; set; }
	public long CandidateMatches { get; set; }
	public long CandidateDifferences { get; set; }
	public string HashSeed { get; set; } = string.Empty;

	public void Apply(StoredTradingEvent item)
	{
		ProcessedEvents++;
		switch (item.Event.EventType)
		{
			case TradingEventTypes.SignalReceived: Signals++; break;
			case TradingEventTypes.StrategyDecisionTaken: StrategyDecisions++; break;
			case TradingEventTypes.RiskDecisionTaken:
				if (PayloadBoolean(item.Event.PayloadJson, "allowed")) RiskAllowed++; else RiskBlocked++;
				break;
			case TradingEventTypes.ExecutionCompleted: ExecutionsCompleted++; break;
			case TradingEventTypes.ExecutionFailed: ExecutionsFailed++; break;
			case TradingEventTypes.PositionOpened: PositionsOpened++; break;
			case TradingEventTypes.PositionClosed: PositionsClosed++; break;
		}
		var material = $"{HashSeed}|{item.GlobalPosition}|{item.Event.EventId:N}|{item.Event.EventType}|{item.Event.PayloadJson}";
		HashSeed = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
	}

	private static bool PayloadBoolean(string json, string property)
	{
		try
		{
			using var document = JsonDocument.Parse(json);
			return document.RootElement.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
		}
		catch { return false; }
	}

	public ReplaySummary ToSummary(Guid replayId)
	{
		var deterministicHash = string.IsNullOrWhiteSpace(HashSeed)
			? Convert.ToHexString(SHA256.HashData(Array.Empty<byte>()))
			: HashSeed;

		return new ReplaySummary(
			replayId,
			ProcessedEvents,
			FailedEvents,
			Signals,
			StrategyDecisions,
			RiskAllowed,
			RiskBlocked,
			ExecutionsCompleted,
			ExecutionsFailed,
			PositionsOpened,
			PositionsClosed,
			CandidateMatches,
			CandidateDifferences,
			deterministicHash);
	}
}
