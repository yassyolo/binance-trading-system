namespace TradingSystem.Observability.History;

public interface ITradingPipelineRecorder
{
    Task RecordSignalAsync(SignalHistoryRecord record, CancellationToken ct);
    
    Task RecordDecisionAsync(DecisionHistoryRecord record, CancellationToken ct);
    
    Task UpsertPositionAsync(PositionHistoryRecord record, CancellationToken ct);
    
    Task RecordPositionEventAsync(PositionEventHistoryRecord record, CancellationToken ct);
    
    Task RecordOrderEventAsync(OrderEventHistoryRecord record, CancellationToken ct);
}
