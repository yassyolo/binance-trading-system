namespace TradingSystem.Backtesting.Models;

public sealed record SymbolTradingRules
{
    public decimal TickSize { get; init; } = 0.1m;
    public decimal QuantityStep { get; init; } = 0.001m;
    public decimal MinimumQuantity { get; init; } = 0.001m;
    public decimal MaximumQuantity { get; init; } = 1_000m;
    public decimal MinimumNotional { get; init; } = 5m;
    public decimal ContractMultiplier { get; init; } = 1m;
    public int Leverage { get; init; } = 20;
}
