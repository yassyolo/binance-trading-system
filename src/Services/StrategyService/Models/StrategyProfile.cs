namespace StrategyService.Models;

public sealed class StrategyProfile
{
    public string Name { get; set; } = string.Empty;

    public bool UseTakeProfit { get; set; }

    public bool UseStopLoss { get; set; }

    public bool UseStop3 { get; set; }

    public decimal TakeProfitPercent { get; set; }

    public decimal StopLossPercent { get; set; }

    public decimal Stop3Percent { get; set; }
}