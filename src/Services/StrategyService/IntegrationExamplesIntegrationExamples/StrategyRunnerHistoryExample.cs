using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.History;

namespace StrategyService.IntegrationExamples;

// Insert the equivalent calls in your existing StrategyRunner. This is deliberately
// an example because the uploaded fragment did not include the current runner contract.
public sealed class StrategyRunnerHistoryExample(ITradingPipelineRecorder recorder)
{
    public Task RecordReceivedAsync(
        string signalId, string botName, string version, string symbol, string side,
        string source, string environment, decimal markPrice, string rawPayload,
        CancellationToken ct)
        => recorder.RecordSignalReceivedAsync(new SignalReceivedRecord(
            signalId, botName, version, symbol, side, source, environment,
            DateTime.UtcNow, markPrice, rawPayload), ct);

    public Task RecordDecisionAsync(
        string signalId, string botName, string version, string symbol, string side,
        string action, string reason, string environment, decimal markPrice,
        IReadOnlyDictionary<string, object?> parameters, CancellationToken ct)
        => recorder.RecordDecisionAsync(new StrategyDecisionRecord(
            signalId, botName, version, symbol, side, action, reason, environment,
            DateTime.UtcNow, markPrice, parameters), ct);
}
