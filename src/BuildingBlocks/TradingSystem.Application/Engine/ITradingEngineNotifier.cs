using TradingSystem.Application.Execution;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Engine;

public interface ITradingEngineNotifier
{
    Task DecisionMadeAsync(
        TradeSignal signal,
        decimal markPrice,
        StrategyDecision decision,
        CancellationToken cancellationToken);

    Task ExecutionCompletedAsync(
        TradeSignal signal,
        TradeExecutionResult result,
        CancellationToken cancellationToken);

    Task ProcessingFailedAsync(
        TradeSignal signal,
        Exception exception,
        CancellationToken cancellationToken);
}
