namespace TradingSystem.Backtesting.Models;

public sealed record BacktestMetrics
{
    public int TotalTrades { get; init; }
    public int WinningTrades { get; init; }
    public int LosingTrades { get; init; }
    public decimal WinRatePercent { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal GrossLoss { get; init; }
    public decimal NetProfit { get; init; }
    public decimal NetReturnPercent { get; init; }
    public decimal ProfitFactor { get; init; }
    public decimal MaximumDrawdownAmount { get; init; }
    public decimal MaximumDrawdownPercent { get; init; }
    public decimal AverageWin { get; init; }
    public decimal AverageLoss { get; init; }
    public decimal Expectancy { get; init; }
    public decimal AverageRMultiple { get; init; }
    public int MaximumConsecutiveWins { get; init; }
    public int MaximumConsecutiveLosses { get; init; }
    public decimal TotalFees { get; init; }
    public decimal TotalFunding { get; init; }
}
