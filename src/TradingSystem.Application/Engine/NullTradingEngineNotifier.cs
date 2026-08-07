using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Execution.Models;
using TradingSystem.Application.Strategies.Models;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Engine;

public sealed class NullTradingEngineNotifier : ITradingEngineNotifier
{
    public Task DecisionMadeAsync(TradeSignal signal,  decimal markPrice,  StrategyDecision decision,  CancellationToken ct) 
        => Task.CompletedTask;
    
    public Task ExecutionCompletedAsync(TradeSignal signal,  TradeExecutionResult result,  CancellationToken ct)  
        => Task.CompletedTask;
    
    public Task ProcessingFailedAsync(TradeSignal signal,  Exception exception,  CancellationToken ct)  
        => Task.CompletedTask;
}
