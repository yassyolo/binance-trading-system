namespace TradingSystem.RiskManagement.Models;

public sealed class RiskBotProfile
{
    public decimal Quantity { get; set; }

    public int Leverage { get; set; } = 1;

    public decimal? MaximumNotional { get; set; }
}