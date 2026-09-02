namespace TradingSystem.Optimization.Models;

public sealed record OptimizationScoreWeights
{
    public decimal ReturnWeight { get; init; } = 1m;
    
    public decimal ProfitFactorWeight { get; init; } = 4m;
  
    public decimal DrawdownPenalty { get; init; } = 1.5m;
   
    public decimal LowActivityPenalty { get; init; } = 2m;
    
    public int MinimumClosedPositions { get; init; } = 10;
    
    public decimal MaximumProfitFactorContribution { get; init; } = 5m;
}
