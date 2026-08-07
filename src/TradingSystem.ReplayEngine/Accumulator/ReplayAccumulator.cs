using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TradingSystem.EventStore.Constants;
using TradingSystem.EventStore.Models;
using TradingSystem.ReplayEngine.Models;

namespace TradingSystem.ReplayEngine.Accumulator;

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
		catch 
		{ 
			return false;
		}
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
