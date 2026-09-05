using TradingSystem.Backtesting.Bots.Bot8016.Models;
using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Bots.Models.Enums;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.MarketData;
using TradingSystem.Strategies.Alligator;
using TradingSystem.Strategies.Protection;

namespace TradingSystem.Backtesting.Bots.Bot8016;

public sealed class Bot8016BacktestEngine(AlligatorEntryPolicy entryPolicy, Stop3Policy stop3Policy)
{
    public BotBacktestResult<Bot8016BacktestOptions> Run(IReadOnlyList<MarketCandle> sourceCandles, IReadOnlyList<HistoricalAlligatorSnapshot> sourceIndicators, Bot8016BacktestOptions o)
    {
        var candles = sourceCandles.OrderBy(x => x.OpenTimeUtc).ToArray(); 
        
        if (candles.Length < 2) 
            throw new InvalidOperationException("At least two candles are required."); 
        
        var indicators = sourceIndicators.OrderBy(x => x.TimeUtc).ToArray(); 
        
        var completed = new List<BotPositionResult>(); 
        var executions = new List<BotExecution>(); 
        var decisions = new List<BotSignalDecision>(); 
        var equity = new List<BotEquityPoint>(); 
        var generated = new List<HistoricalBotSignal>(); 
        P? active = null; 
        decimal balance = o.InitialBalance, peak = balance;      
        HistoricalAlligatorSnapshot? latest = null; var ii = 0;
        foreach (var c in candles)
        {
            while (ii < indicators.Length && indicators[ii].TimeUtc <= c.CloseTimeUtc) latest = indicators[ii++];
            if (c.Interval.Equals(o.EntryTimeframe, StringComparison.OrdinalIgnoreCase) && latest is not null && active is null)
            {
                var decision = entryPolicy.Evaluate(new(c.Symbol, c.Interval, c.IsClosed, c.Open, c.High, c.Low, c.Close, latest.Teeth, latest.Sma200), new(o.Symbol, o.EntryTimeframe, o.EnableLong, o.EnableShort, o.UseMa200Filter, o.MinimumSignalCandleRange));
                if (decision is not null)
                {
                    var side = decision.Side == PositionSide.Long
                        ? TradeSide.Long
                        : TradeSide.Short;

                    var signal = new HistoricalBotSignal(
                        c.CloseTimeUtc,
                        side,
                        "BOT8016_INTERNAL");

                    generated.Add(signal);

                    var entry = BotBacktestMath.RoundToStep(
                        BotBacktestMath.EntrySlippage(
                            c.Close,
                            side,
                            o.SlippageBasisPoints),
                        o.TickSize);

                    var sl = decision.Side == PositionSide.Long
                        ? c.Low > 0
                            ? c.Low
                            : entry - o.InitialStopLossFallback
                        : c.High > 0
                            ? c.High
                            : entry + o.InitialStopLossFallback;

                    var tp = decision.Side == PositionSide.Long
                        ? entry * (1 + o.TakeProfitPercent / 100m)
                        : entry * (1 - o.TakeProfitPercent / 100m);

                    var fee = entry * o.Quantity * o.TakerFeeRate;

                    balance -= fee;

                    active = new P(
                        Guid.NewGuid().ToString("N")[..8],
                        decision.Side,
                        c.CloseTimeUtc,
                        entry,
                        o.Quantity,
                        sl,
                        tp,
                        c.High,
                        c.Low,
                        fee);

                    executions.Add(new BotExecution(
                        active.Id,
                        c.CloseTimeUtc,
                        "ENTRY",
                        side,
                        entry,
                        o.Quantity,
                        0,
                        fee,
                        decision.Reason));

                    decisions.Add(new BotSignalDecision(
                        c.CloseTimeUtc,
                        side,
                        "Open",
                        decision.Reason,
                        c.Close,
                        signal.SignalId));
                }
            }
            if (active is not null)
            {
                var side = active.Side == PositionSide.Long ? TradeSide.Long : TradeSide.Short; 
                
                if (!active.TpReached) 
                { 
                    var slHit = active.Side == PositionSide.Long ? c.Low <= active.Sl : c.High >= active.Sl; 
                    var tpHit = active.Side == PositionSide.Long ? c.High >= active.Tp : c.Low <= active.Tp; 
                    
                    if (slHit) { Close(active, active.Sl, c.CloseTimeUtc, "INITIAL_SL"); active = null; } else if (tpHit) { var qty = active.Qty / 2m; Realize(active, active.Tp, qty, c.CloseTimeUtc, "TP_PARTIAL"); active.TpReached = true; active.Remaining -= qty; } }
                if (active is not null && active.TpReached && !active.Stop3Created && stop3Policy.Breakout(active.Side, c.Close, active.SignalHigh, active.SignalLow)) { active.Stop3 = BotBacktestMath.RoundToStep(stop3Policy.InitialTrigger(active.Side, active.Entry, new(o.Stop3EntryOffset, 0, 0)), o.TickSize); active.Stop3Created = true; }
                if (active is not null && active.Stop3Created && active.Stop3.HasValue) { var hit = active.Side == PositionSide.Long ? c.Low <= active.Stop3 : c.High >= active.Stop3; if (hit) { Close(active, active.Stop3.Value, c.CloseTimeUtc, "STOP3"); active = null; } else if (latest is not null && stop3Policy.TeethExit(active.Side, c.Close, latest.Teeth)) { Close(active, c.Close, c.CloseTimeUtc, active.Side == PositionSide.Long ? "EXIT_BELOW_TEETH" : "EXIT_ABOVE_TEETH"); active = null; } }
            }
            var unreal = active is null ? 0 : BotBacktestMath.UnrealizedPnl(active.Side == PositionSide.Long ? TradeSide.Long : TradeSide.Short, active.Entry, c.Close, active.Remaining); var eq = balance + unreal; peak = Math.Max(peak, eq); var dd = peak - eq; equity.Add(new(c.CloseTimeUtc, balance, eq, peak, dd, peak == 0 ? 0 : dd / peak * 100m));
        }
        if (active is not null) { var c = candles[^1]; Close(active, c.Close, c.CloseTimeUtc, "BACKTEST_END"); }
        return new() { RunId = $"BOT8016_{o.Symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}", BotName = o.BotName, Options = o, StartedAtUtc = candles[0].OpenTimeUtc, CompletedAtUtc = DateTime.UtcNow, Metrics = BotBacktestMath.Metrics(generated.Count, decisions.Count(x => x.Decision == "Block"), o.InitialBalance, balance, completed, equity), Positions = completed, Executions = executions, Decisions = decisions, EquityCurve = equity, Candles = candles, Signals = generated };
        void Realize(P p, decimal raw, decimal qty, DateTime time, string reason) { var side = p.Side == PositionSide.Long ? TradeSide.Long : TradeSide.Short; var price = BotBacktestMath.RoundToStep(BotBacktestMath.ExitSlippage(raw, side, o.SlippageBasisPoints), o.TickSize); var gross = BotBacktestMath.UnrealizedPnl(side, p.Entry, price, qty); var fee = price * qty * o.TakerFeeRate; p.Gross += gross; p.Fees += fee; balance += gross - fee; executions.Add(new(p.Id, time, "EXIT", side, price, qty, gross, fee, reason)); }
        void Close(P p, decimal raw, DateTime time, string reason) { if (p.Remaining > 0) { Realize(p, raw, p.Remaining, time, reason); p.Remaining = 0; } var side = p.Side == PositionSide.Long ? TradeSide.Long : TradeSide.Short; completed.Add(new() { PositionId = p.Id, Side = side, EntryTimeUtc = p.EntryTime, EntryPrice = p.Entry, ExitTimeUtc = time, ExitPrice = raw, Quantity = p.Qty, GrossPnl = p.Gross, Fees = p.Fees, NetPnl = p.Gross - p.Fees, ExitReason = reason, PartialTakeProfitReached = p.TpReached }); }
    }
    private sealed class P(string id, PositionSide side, DateTime entryTime, decimal entry, decimal qty, decimal sl, decimal tp, decimal high, decimal low, decimal fee) { public string Id = id; public PositionSide Side = side; public DateTime EntryTime = entryTime; public decimal Entry = entry; public decimal Qty = qty; public decimal Remaining = qty; public decimal Sl = sl; public decimal Tp = tp; public decimal? SignalHigh = high; public decimal? SignalLow = low; public decimal Fees = fee; public decimal Gross; public bool TpReached; public bool Stop3Created; public decimal? Stop3; }
}