using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Costs;

public interface ITradingCostModel
{
    decimal CalculateEntryFee(decimal price,  decimal quantity);
    decimal CalculateExitFee(decimal price,  decimal quantity);
    decimal CalculateFunding(BacktestPosition position,  DateTime exitTimeUtc);
    decimal ApplyEntrySlippage(decimal price,  TradeSide side,  decimal basisPoints);
    decimal ApplyExitSlippage(decimal price,  TradeSide side,  decimal basisPoints);
}
