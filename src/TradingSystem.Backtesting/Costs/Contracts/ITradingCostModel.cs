using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Costs.Contracts;

public interface ITradingCostModel
{
    decimal CalculateEntryFee(decimal price,  decimal quantity);
   
    decimal CalculateExitFee(decimal price,  decimal quantity);
   
    decimal CalculateFunding(BacktestPosition position,  DateTime exitTimeUtc);
   
    decimal ApplyEntrySlippage(decimal price,  TradeSide side,  decimal basisPoints);
   
    decimal ApplyExitSlippage(decimal price,  TradeSide side,  decimal basisPoints);
}
