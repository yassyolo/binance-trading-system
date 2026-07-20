using TradingSystem.Application.Execution;
using TradingSystem.Application.Strategies;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Engine;

public sealed class NullTradingEngineNotifier : ITradingEngineNotifier
{
    public Task DecisionMadeAsync(TradeSignal signal,  decimal markPrice,  StrategyDecision decision,  CancellationToken cancellationToken)  =>  Task.CompletedTask;
    public Task ExecutionCompletedAsync(TradeSignal signal,  TradeExecutionResult result,  CancellationToken cancellationToken)  =>  Task.CompletedTask;
    public Task ProcessingFailedAsync(TradeSignal signal,  Exception exception,  CancellationToken cancellationToken)  =>  Task.CompletedTask;
}
