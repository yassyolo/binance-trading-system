using TradingSystem.Backtesting.Bot8012.Models;
using TradingSystem.Backtesting.Bot8012.Policies;
namespace TradingSystem.Backtesting.Bot8012.Engine;

public sealed class Bot8012BacktestEngine
{
    public Bot8012BacktestResult Run(IReadOnlyList<Candle> candles, IReadOnlyList<HistoricalSignal> signals, Bot8012BacktestOptions o)
    {
        if (candles.Count < 2) throw new ArgumentException("At least two candles are required.");
        var ordered = candles.OrderBy(x => x.OpenTimeUtc).ToArray(); var sig = signals.OrderBy(x => x.TimeUtc).ToArray();
        var positions = new List<SimulatedPosition>(); var decisions = new List<SignalDecision>(); var equity = new List<EquityPoint>();
        var pending = new List<HistoricalSignal>(); var last = new Dictionary<BacktestSide, DateTime>(); decimal balance = o.InitialBalance, peak = balance; int si = 0;
        var policy = new Bot8012BacktestGapPolicy(o);
        for (int i = 0; i < ordered.Length; i++)
        {
            var c = ordered[i];
            // signals become known only at/after their timestamp; execution is next candle open
            while (si < sig.Length && sig[si].TimeUtc <= c.OpenTimeUtc) { pending.Add(sig[si++]); }
            foreach (var s in pending.ToArray())
            {
                var enabled = s.Side == BacktestSide.Long ? o.EnableLong : o.EnableShort;
                if (!enabled) { decisions.Add(new(s.TimeUtc, s.Side, "Block", $"{s.Side.ToString().ToUpperInvariant()} is disabled.", c.Open)); pending.Remove(s); continue; }
                if (last.TryGetValue(s.Side, out var at) && s.TimeUtc - at < TimeSpan.FromSeconds(o.CooldownSeconds)) { decisions.Add(new(s.TimeUtc, s.Side, "Block", "SIGNAL_COOLDOWN active.", c.Open)); pending.Remove(s); continue; }
                var check = policy.Validate(s.Side, c.Open, positions);
                decisions.Add(new(s.TimeUtc, s.Side, check.Open ? "Open" : "Block", check.Reason, c.Open)); pending.Remove(s);
                if (!check.Open) continue;
                var entry = ApplySlippage(c.Open, s.Side, true, o.SlippageBasisPoints); var tp = s.Side == BacktestSide.Long ? entry + o.ProfitDistance : entry - o.ProfitDistance;
                var fee = entry * o.Quantity * o.EntryFeeRate;
                positions.Add(new SimulatedPosition { Id = Guid.NewGuid().ToString("N")[..8], Side = s.Side, SignalTimeUtc = s.TimeUtc, EntryTimeUtc = c.OpenTimeUtc, EntryPrice = entry, TpPrice = tp, Quantity = o.Quantity, EntryFee = fee });
                balance -= fee; last[s.Side] = s.TimeUtc;
            }
            foreach (var p in positions.Where(x => !x.Closed).ToArray())
            {
                var hit = p.Side == BacktestSide.Long ? c.High >= p.TpPrice : c.Low <= p.TpPrice;
                if (hit) Close(p, c.CloseTimeUtc, p.TpPrice, "TAKE_PROFIT", ref balance, o);
            }
            var unreal = positions.Where(x => !x.Closed).Sum(p => Pnl(p, c.Close)); var eq = balance + unreal; if (eq > peak) peak = eq; var dd = peak <= 0 ? 0 : (peak - eq) / peak * 100m; equity.Add(new(c.CloseTimeUtc, balance, eq, dd));
        }
        if (o.ForceCloseAtEnd) { var c = ordered[^1]; foreach (var p in positions.Where(x => !x.Closed)) Close(p, c.CloseTimeUtc, ApplySlippage(c.Close, p.Side, false, o.SlippageBasisPoints), "BACKTEST_END", ref balance, o); }
        return new(o, positions, decisions, equity, Metrics(signals.Count, positions, decisions, equity, balance));
    }
    private static void Close(SimulatedPosition p, DateTime time, decimal price, string reason, ref decimal balance, Bot8012BacktestOptions o) { p.ExitTimeUtc = time; p.ExitPrice = price; p.ExitReason = reason; p.GrossPnl = Pnl(p, price); p.ExitFee = price * p.Quantity * o.ExitFeeRate; balance += p.GrossPnl - p.ExitFee; }
    private static decimal Pnl(SimulatedPosition p, decimal price) => (p.Side == BacktestSide.Long ? price - p.EntryPrice : p.EntryPrice - price) * p.Quantity;
    private static decimal ApplySlippage(decimal price, BacktestSide side, bool entry, decimal bps) { var d = price * bps / 10_000m; var adverse = (entry && side == BacktestSide.Long) || (!entry && side == BacktestSide.Short); return adverse ? price + d : price - d; }
    private static BacktestMetrics Metrics(int signals, List<SimulatedPosition> p, List<SignalDecision> d, List<EquityPoint> e, decimal bal) { var closed = p.Where(x => x.Closed).ToArray(); var win = closed.Count(x => x.NetPnl > 0); var lose = closed.Count(x => x.NetPnl < 0); var gp = closed.Where(x => x.NetPnl > 0).Sum(x => x.NetPnl); var gl = Math.Abs(closed.Where(x => x.NetPnl < 0).Sum(x => x.NetPnl)); return new(signals, p.Count, d.Count(x => x.Decision == "Block"), closed.Length, win, lose, closed.Length == 0 ? 0 : (decimal)win / closed.Length * 100m, closed.Sum(x => x.NetPnl), closed.Sum(x => x.EntryFee + x.ExitFee), gl == 0 ? (gp > 0 ? decimal.MaxValue : 0) : gp / gl, e.Count == 0 ? 0 : e.Max(x => x.DrawdownPercent), bal); }
}
