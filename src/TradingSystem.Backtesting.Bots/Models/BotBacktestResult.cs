using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Models;

public sealed record BotBacktestResult<TOptions>
{
    public required string RunId { get; init; }
    
    public required string BotName { get; init; }
   
    public required TOptions Options { get; init; }
    
    public required DateTime StartedAtUtc { get; init; }
    
    public required DateTime CompletedAtUtc { get; init; }
   
    public required BotBacktestMetrics Metrics { get; init; }
   
    public required IReadOnlyList<BotPositionResult> Positions { get; init; }
    
    public required IReadOnlyList<BotExecution> Executions { get; init; }
    
    public required IReadOnlyList<BotSignalDecision> Decisions { get; init; }
   
    public required IReadOnlyList<BotEquityPoint> EquityCurve { get; init; }
    
    public required IReadOnlyList<MarketCandle> Candles { get; init; }
    
    public required IReadOnlyList<HistoricalBotSignal> Signals { get; init; }
}
