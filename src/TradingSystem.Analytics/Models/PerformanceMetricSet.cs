namespace TradingSystem.Analytics.Models;

public sealed record PerformanceMetricSet
{
    public int Signals { get; init; }
   
    public int OpenedPositions { get; init; }
   
    public int BlockedSignals { get; init; }
    
    public int ClosedPositions { get; init; }
    
    public int WinningPositions { get; init; }
   
    public int LosingPositions { get; init; }
    
    public decimal InitialBalance { get; init; }
    
    public decimal FinalBalance { get; init; }
   
    public decimal NetProfit { get; init; }
    
    public decimal ReturnPercent { get; init; }
    
    public decimal WinRatePercent { get; init; }
    
    public decimal ProfitFactor { get; init; }
   
    public decimal MaximumDrawdownAmount { get; init; }
    
    public decimal MaximumDrawdownPercent { get; init; }
    
    public decimal TotalFees { get; init; }
    
    public decimal Expectancy { get; init; }
}
