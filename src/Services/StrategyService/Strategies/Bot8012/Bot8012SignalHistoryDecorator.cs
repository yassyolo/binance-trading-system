using TradingSystem.Observability.Models;
using TradingSystem.Observability.Services;

namespace StrategyService.Strategies.Bot8012;

// Call this around the existing StrategyRunner flow. It deliberately records received AND blocked signals.
public sealed class Bot8012SignalHistoryDecorator(TradingHistoryRecorder history)
{
    public Task SignalReceivedAsync(string version, string symbol, string side, string source, string rawPayload, decimal markPrice, string signalId, CancellationToken ct)
        => history.RecordAsync(TradingHistoryEventType.SignalReceived, "BOT8012", version, symbol, DateTime.UtcNow, side, signalId: signalId, source: source, markPrice: markPrice, rawPayload: rawPayload, cancellationToken: ct);

    public Task DecisionAsync(string version, string symbol, string side, string signalId, string decision, string reason, decimal markPrice, CancellationToken ct)
        => history.RecordAsync(TradingHistoryEventType.StrategyDecision, "BOT8012", version, symbol, DateTime.UtcNow, side, signalId: signalId, decision: decision, reason: reason, markPrice: markPrice, cancellationToken: ct);
}
