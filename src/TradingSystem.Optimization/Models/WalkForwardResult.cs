namespace TradingSystem.Optimization.Models;

public sealed record WalkForwardResult<TOptions>
{
    public required string BotName { get; init; }
    
    public required DateTime StartedAtUtc { get; init; }
   
    public required DateTime CompletedAtUtc { get; init; }
   
    public required IReadOnlyList<WalkForwardWindowResult<TOptions>> Windows { get; init; }
   
    public decimal AverageOutOfSampleScore => Windows.Count == 0 ? 0m : Windows.Average(x => x.OutOfSampleScore);
    
    public decimal TotalOutOfSampleNetProfit => Windows.Sum(x => x.OutOfSampleMetrics.NetProfit);
    
    public decimal WorstOutOfSampleDrawdownPercent => Windows.Count == 0 ? 0m : Windows.Max(x => x.OutOfSampleMetrics.MaximumDrawdownPercent);
}
