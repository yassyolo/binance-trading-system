using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Execution;

public sealed record ProtectiveFill(ExitReason Reason, decimal Price);

public sealed class CandleFillResolver
{
    public ProtectiveFill? Resolve(BacktestPosition position, HistoricalCandle candle, IntrabarConflictPolicy policy)
    {
        var stopHit = position.StopLoss.HasValue && IsPriceTouched(position.StopLoss.Value, position.Side, isStop: true, candle);
        var takeHit = position.TakeProfit.HasValue && IsPriceTouched(position.TakeProfit.Value, position.Side, isStop: false, candle);

        if (!stopHit && !takeHit) return null;
        if (stopHit && !takeHit) return new(ExitReason.StopLoss, position.StopLoss!.Value);
        if (!stopHit && takeHit) return new(ExitReason.TakeProfit, position.TakeProfit!.Value);

        return policy switch
        {
            IntrabarConflictPolicy.StopLossFirst => new(ExitReason.StopLoss, position.StopLoss!.Value),
            IntrabarConflictPolicy.TakeProfitFirst => new(ExitReason.TakeProfit, position.TakeProfit!.Value),
            IntrabarConflictPolicy.WorstCase => new(ExitReason.StopLoss, position.StopLoss!.Value),
            IntrabarConflictPolicy.BestCase => new(ExitReason.TakeProfit, position.TakeProfit!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
        };
    }

    private static bool IsPriceTouched(decimal price, TradeSide side, bool isStop, HistoricalCandle candle)
    {
        if (side == TradeSide.Long)
            return isStop ? candle.Low <= price : candle.High >= price;
        return isStop ? candle.High >= price : candle.Low <= price;
    }
}
