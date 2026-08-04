namespace TradingSystem.Observability.History;

public sealed class NullTradingPipelineRecorder : ITradingPipelineRecorder
{
    public Task RecordSignalAsync(SignalHistoryRecord r, CancellationToken ct)
        => Task.CompletedTask;
    
    public Task RecordDecisionAsync(DecisionHistoryRecord r, CancellationToken ct) 
        => Task.CompletedTask;
    
    public Task UpsertPositionAsync(PositionHistoryRecord r, CancellationToken ct) 
        => Task.CompletedTask;
   
    public Task RecordPositionEventAsync(PositionEventHistoryRecord r, CancellationToken ct) 
        => Task.CompletedTask;
    
    public Task RecordOrderEventAsync(OrderEventHistoryRecord r, CancellationToken ct) 
        => Task.CompletedTask;
}
