using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Risk;

public sealed record PositionSizeResult(bool Succeeded, decimal Quantity, string Reason)
{
    public static PositionSizeResult Success(decimal quantity) => new(true, quantity, string.Empty);
    public static PositionSizeResult Failure(string reason) => new(false, 0m, reason);
}

public sealed class PositionSizeCalculator
{
    public PositionSizeResult Calculate(decimal balance, decimal riskPercent, decimal entryPrice, decimal stopPrice, SymbolTradingRules rules)
    {
        var distance = Math.Abs(entryPrice - stopPrice);
        if (distance <= 0m) return PositionSizeResult.Failure("Stop distance must be positive.");

        var riskAmount = balance * riskPercent / 100m;
        var byRisk = riskAmount / (distance * rules.ContractMultiplier);
        var byMargin = balance * rules.Leverage / (entryPrice * rules.ContractMultiplier);
        var quantity = Math.Min(Math.Min(byRisk, byMargin), rules.MaximumQuantity);
        quantity = RoundDown(quantity, rules.QuantityStep);

        if (quantity < rules.MinimumQuantity)
            return PositionSizeResult.Failure("Minimum quantity would exceed configured risk or margin.");

        var notional = quantity * entryPrice * rules.ContractMultiplier;
        if (notional < rules.MinimumNotional)
            return PositionSizeResult.Failure("Order is below minimum notional.");

        return PositionSizeResult.Success(quantity);
    }

    private static decimal RoundDown(decimal value, decimal step)
        => step <= 0m ? value : Math.Floor(value / step) * step;
}
