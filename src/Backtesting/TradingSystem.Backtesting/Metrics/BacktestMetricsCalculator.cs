using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Metrics;

public sealed class BacktestMetricsCalculator
{
    public BacktestMetrics Calculate(decimal initialBalance, decimal finalBalance, IReadOnlyList<BacktestTrade> trades, IReadOnlyList<EquityPoint> equity)
    {
        var wins = trades.Where(x => x.NetPnl > 0m).ToArray();
        var losses = trades.Where(x => x.NetPnl <= 0m).ToArray();
        var grossProfit = wins.Sum(x => x.NetPnl);
        var grossLoss = Math.Abs(losses.Sum(x => x.NetPnl));
        var (maxWins, maxLosses) = Consecutive(trades);
        return new BacktestMetrics
        {
            TotalTrades = trades.Count,
            WinningTrades = wins.Length,
            LosingTrades = losses.Length,
            WinRatePercent = trades.Count == 0 ? 0m : wins.Length * 100m / trades.Count,
            GrossProfit = grossProfit,
            GrossLoss = grossLoss,
            NetProfit = finalBalance - initialBalance,
            NetReturnPercent = initialBalance == 0m ? 0m : (finalBalance - initialBalance) / initialBalance * 100m,
            ProfitFactor = grossLoss == 0m ? (grossProfit > 0m ? decimal.MaxValue : 0m) : grossProfit / grossLoss,
            MaximumDrawdownAmount = equity.Count == 0 ? 0m : equity.Max(x => x.DrawdownAmount),
            MaximumDrawdownPercent = equity.Count == 0 ? 0m : equity.Max(x => x.DrawdownPercent),
            AverageWin = wins.Length == 0 ? 0m : wins.Average(x => x.NetPnl),
            AverageLoss = losses.Length == 0 ? 0m : losses.Average(x => x.NetPnl),
            Expectancy = trades.Count == 0 ? 0m : trades.Average(x => x.NetPnl),
            AverageRMultiple = trades.Count == 0 ? 0m : trades.Average(x => x.RMultiple),
            MaximumConsecutiveWins = maxWins,
            MaximumConsecutiveLosses = maxLosses,
            TotalFees = trades.Sum(x => x.EntryFee + x.ExitFee),
            TotalFunding = trades.Sum(x => x.FundingCost)
        };
    }

    private static (int wins, int losses) Consecutive(IReadOnlyList<BacktestTrade> trades)
    {
        var maxW = 0; var maxL = 0; var w = 0; var l = 0;
        foreach (var trade in trades)
        {
            if (trade.NetPnl > 0m) { w++; l = 0; maxW = Math.Max(maxW, w); }
            else { l++; w = 0; maxL = Math.Max(maxL, l); }
        }
        return (maxW, maxL);
    }
}
