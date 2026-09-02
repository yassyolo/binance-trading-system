using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Bots.Common;

internal static class BotBacktestMath
{
    public static decimal RoundToStep(decimal value, decimal step)
         =>  step <= 0 ? value : Math.Round(value / step, MidpointRounding.AwayFromZero) * step;

    public static decimal RoundDown(decimal value,  decimal step)
         =>  step <= 0 ? value : Math.Floor(value / step) * step;

    public static decimal EntrySlippage(decimal price, TradeSide side, decimal basisPoints)
         =>  side == TradeSide.Long
            ? price * (1m + basisPoints / 10_000m)
            : price * (1m - basisPoints / 10_000m);

    public static decimal ExitSlippage(decimal price, TradeSide side, decimal basisPoints)
         =>  side == TradeSide.Long
            ? price * (1m - basisPoints / 10_000m)
            : price * (1m + basisPoints / 10_000m);

    public static decimal UnrealizedPnl(TradeSide side, decimal entry, decimal current, decimal quantity)
         =>  (side == TradeSide.Long ? current - entry : entry - current) * quantity;

    public static BotBacktestMetrics Metrics(
        int signals, 
        int blocked, 
        decimal initial, 
        decimal final, 
        IReadOnlyList<BotPositionResult> positions, 
        IReadOnlyList<BotEquityPoint> equity)
    {
        var wins = positions.Where(x  =>  x.NetPnl > 0).ToArray();
        var losses = positions.Where(x  =>  x.NetPnl <= 0).ToArray();
        var grossProfit = wins.Sum(x  =>  x.NetPnl);
        var grossLoss = Math.Abs(losses.Sum(x  =>  x.NetPnl));
        return new BotBacktestMetrics
        {
            Signals = signals, 
            OpenedPositions = positions.Count, 
            BlockedSignals = blocked, 
            ClosedPositions = positions.Count, 
            WinningPositions = wins.Length, 
            LosingPositions = losses.Length, 
            WinRatePercent = positions.Count == 0 ? 0 : wins.Length * 100m / positions.Count, 
            InitialBalance = initial, 
            FinalBalance = final, 
            NetProfit = final - initial, 
            ReturnPercent = initial == 0 ? 0 : (final - initial) / initial * 100m, 
            GrossProfit = grossProfit, 
            GrossLoss = grossLoss, 
            ProfitFactor = grossLoss == 0 ? (grossProfit > 0 ? decimal.MaxValue : 0) : grossProfit / grossLoss, 
            MaximumDrawdownAmount = equity.Count == 0 ? 0 : equity.Max(x  =>  x.DrawdownAmount), 
            MaximumDrawdownPercent = equity.Count == 0 ? 0 : equity.Max(x  =>  x.DrawdownPercent), 
            TotalFees = positions.Sum(x  =>  x.Fees), 
            Expectancy = positions.Count == 0 ? 0 : positions.Average(x  =>  x.NetPnl), 
            PartialTakeProfits = positions.Count(x  =>  x.PartialTakeProfitReached)
        };
    }
}
