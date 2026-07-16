using TradingSystem.Signals.History;

namespace TradingSystem.Signals.Abstractions;

public interface ITradingPipelineRecorder
{
    Task RecordSignalReceivedAsync(SignalReceivedRecord record, CancellationToken cancellationToken);
    Task RecordDecisionAsync(StrategyDecisionRecord record, CancellationToken cancellationToken);
    Task UpsertPositionAsync(PositionHistoryRecord record, CancellationToken cancellationToken);
    Task RecordPositionEventAsync(PositionEventRecord record, CancellationToken cancellationToken);
    Task RecordOrderEventAsync(OrderEventRecord record, CancellationToken cancellationToken);
}
