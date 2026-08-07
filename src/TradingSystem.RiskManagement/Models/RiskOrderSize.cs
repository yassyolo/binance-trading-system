namespace TradingSystem.RiskManagement.Models;

public sealed record RiskOrderSize(decimal Quantity, int Leverage, decimal? MaximumNotional);

