namespace TradingSystem.Backtesting.Risk.Models;

public sealed record PositionSizeResult(bool Succeeded, decimal Quantity, string Reason)
{
    public static PositionSizeResult Success(decimal quantity) => new(true, quantity, string.Empty);
    public static PositionSizeResult Failure(string reason) => new(false, 0m, reason);
}

