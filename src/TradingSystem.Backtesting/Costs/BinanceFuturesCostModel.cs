using TradingSystem.Backtesting.Costs.Contracts;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Costs;

public sealed class BinanceFuturesCostModel(decimal takerFeeRate = 0.0004m) : ITradingCostModel
{
    public decimal CalculateEntryFee(decimal price, decimal quantity)  
        => price * quantity * takerFeeRate;
    
    public decimal CalculateExitFee(decimal price, decimal quantity)  
        => price * quantity * takerFeeRate;
    
    public decimal CalculateFunding(BacktestPosition position,  DateTime exitTimeUtc)  
        => 0m;

    public decimal ApplyEntrySlippage(decimal price,  TradeSide side,  decimal basisPoints)
         => side == TradeSide.Long ? price * (1m + basisPoints / 10_000m) : price * (1m - basisPoints / 10_000m);

    public decimal ApplyExitSlippage(decimal price,  TradeSide side,  decimal basisPoints)
         => side == TradeSide.Long ? price * (1m - basisPoints / 10_000m) : price * (1m + basisPoints / 10_000m);
}
