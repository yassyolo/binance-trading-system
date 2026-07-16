using TradingSystem.Backtesting.Bot8012.Models;
namespace TradingSystem.Backtesting.Bot8012.Policies;

public sealed class Bot8012BacktestGapPolicy(Bot8012BacktestOptions options)
{
    public (bool Open, string Reason) Validate(BacktestSide side, decimal markPrice, IReadOnlyCollection<SimulatedPosition> active)
    {
        var same = active.Where(x => !x.Closed && x.Side == side).ToArray();
        if (same.Length >= options.OrderSideLimit) return (false, $"ORDER_SIDE_LIMIT reached ({same.Length}/{options.OrderSideLimit}).");
        if (same.Length == 0) return (true, "No active TP positions for this side.");
        var newest = same.OrderByDescending(x => x.EntryTimeUtc).First();
        var newestTp = Round(newest.TpPrice); var mark = Round(markPrice); var profit = Round(options.ProfitDistance); var gap = Round(options.PriceDistance);
        if (side == BacktestSide.Long) { var lastEntry = newestTp - profit; var max = lastEntry - gap; return mark > max ? (false, $"GAP fail LONG: mark={mark}, newestTp={newestTp}, requiredMaximum={max}.") : (true, $"LONG spacing valid: mark={mark}, requiredMaximum={max}."); }
        var shortEntry = newestTp + profit; var min = shortEntry + gap; return mark < min ? (false, $"GAP fail SHORT: mark={mark}, newestTp={newestTp}, requiredMinimum={min}.") : (true, $"SHORT spacing valid: mark={mark}, requiredMinimum={min}.");
    }
    private static decimal Round(decimal v) => Math.Round(v, 0, MidpointRounding.ToEven);
}
