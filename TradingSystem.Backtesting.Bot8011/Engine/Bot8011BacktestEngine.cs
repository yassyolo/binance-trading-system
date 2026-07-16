using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Bot8011.Models;

namespace TradingSystem.Backtesting.Bot8011.Engine;

public sealed class Bot8011BacktestEngine
{
    public Task<Bot8011BacktestResult> RunAsync(decimal initialBalance, Bot8011BacktestOptions options,
        IReadOnlyList<HistoricalCandle> sourceCandles, IReadOnlyList<Bot8011Signal> sourceSignals,
        CancellationToken cancellationToken = default)
    {
        Validate(options, sourceCandles);
        var candles = sourceCandles.OrderBy(x => x.OpenTimeUtc).ToArray();
        var signals = sourceSignals.OrderBy(x => x.TimeUtc).ToArray();
        var executions = new List<Bot8011Execution>();
        var positions = new List<Bot8011PositionResult>();
        var equity = new List<Bot8011EquityPoint>();
        var signalIndex = 0;
        var balance = initialBalance;
        var peak = balance;
        Bot8011SimulatedPosition? active = null;
        PendingSignal? pending = null;
        var lastSignalAt = new Dictionary<TradeSide, DateTime>();

        foreach (var candle in candles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pending is not null)
            {
                if (active is null) active = Open(pending.Signal, candle.Open, candle.OpenTimeUtc);
                pending = null;
            }

            if (active is not null)
            {
                ProcessProtection(active, candle);
                if (active.Stage != Bot8011LifecycleStage.Closed && active.Stage == Bot8011LifecycleStage.Stop3Active)
                    UpdateTrailing(active, candle.Close);
            }

            while (signalIndex < signals.Length && signals[signalIndex].TimeUtc <= candle.CloseTimeUtc)
            {
                var signal = signals[signalIndex++];
                if (signal.TimeUtc < candle.OpenTimeUtc) continue;
                if ((signal.Side == TradeSide.Long && !options.EnableLong) || (signal.Side == TradeSide.Short && !options.EnableShort)) continue;
                if (lastSignalAt.TryGetValue(signal.Side, out var last) && signal.TimeUtc - last < TimeSpan.FromSeconds(options.CooldownSeconds)) continue;
                lastSignalAt[signal.Side] = signal.TimeUtc;

                if (active is not null && active.Side != signal.Side)
                {
                    CloseRemaining(active, ApplyExitSlippage(candle.Close, active.Side), candle.CloseTimeUtc, "REVERSE_SIGNAL");
                    active = null;
                }
                else if (active is not null)
                {
                    continue; // one active position: mirrors OrderSideLimit=1
                }

                if (options.EnterOnNextCandleOpen) pending = new(signal);
                else active = Open(signal, candle.Close, candle.CloseTimeUtc);
            }

            peak = Math.Max(peak, balance);
            var drawdown = peak - balance;
            equity.Add(new(candle.CloseTimeUtc, balance, peak, drawdown, peak == 0m ? 0m : drawdown / peak * 100m));
        }

        if (active is not null)
        {
            var last = candles[^1];
            CloseRemaining(active, ApplyExitSlippage(last.Close, active.Side), last.CloseTimeUtc, "BACKTEST_END");
        }

        var metrics = CalculateMetrics(initialBalance, balance, positions, equity);
        return Task.FromResult(new Bot8011BacktestResult
        {
            RunId = $"BOT8011_{options.Symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}",
            Options = options,
            Metrics = metrics,
            Positions = positions,
            Executions = executions,
            EquityCurve = equity,
            Candles = candles,
            Signals = signals
        });

        Bot8011SimulatedPosition? Open(Bot8011Signal signal, decimal rawPrice, DateTime time)
        {
            var quantity = RoundDown(options.Quantity, options.QuantityStep);
            var price = Round(ApplyEntrySlippage(rawPrice, signal.Side), options.TickSize);
            if (quantity < options.MinimumQuantity || price * quantity < options.MinimumNotional) return null;
            var margin = price * quantity / options.Leverage;
            if (margin > balance) return null;
            var sl = signal.Side == TradeSide.Long ? price * (1m - options.StopLossPercent / 100m) : price * (1m + options.StopLossPercent / 100m);
            var tp = signal.Side == TradeSide.Long ? price * (1m + options.TakeProfitPercent / 100m) : price * (1m - options.TakeProfitPercent / 100m);
            var fee = price * quantity * options.TakerFeeRate;
            balance -= fee;
            var p = new Bot8011SimulatedPosition
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Side = signal.Side,
                EntryTimeUtc = time,
                EntryPrice = price,
                InitialQuantity = quantity,
                RemainingQuantity = quantity,
                InitialStopLoss = Round(sl, options.TickSize),
                TakeProfit = Round(tp, options.TickSize),
                Fees = fee,
                MarginUsed = margin
            };
            executions.Add(new(p.Id, time, "ENTRY", p.Side, price, quantity, 0m, fee, signal.Source));
            return p;
        }

        void ProcessProtection(Bot8011SimulatedPosition p, HistoricalCandle candle)
        {
            if (p.Stage == Bot8011LifecycleStage.InitialProtection)
            {
                var slHit = p.Side == TradeSide.Long ? candle.Low <= p.InitialStopLoss : candle.High >= p.InitialStopLoss;
                var tpHit = p.Side == TradeSide.Long ? candle.High >= p.TakeProfit : candle.Low <= p.TakeProfit;
                if (slHit && tpHit && options.WorstCaseWhenTpAndSlTouched) tpHit = false;
                if (slHit) { CloseRemaining(p, ApplyExitSlippage(p.InitialStopLoss, p.Side), candle.CloseTimeUtc, "INITIAL_SL"); active = null; return; }
                if (tpHit)
                {
                    var closeQuantity = RoundDown(p.InitialQuantity * options.TakeProfitCloseFraction, options.QuantityStep);
                    closeQuantity = Math.Min(closeQuantity, p.RemainingQuantity);
                    Realize(p, p.TakeProfit, closeQuantity, candle.CloseTimeUtc, "TP_PARTIAL");
                    p.TpExecuted = true;
                    p.Stop3Current = Round(p.Side == TradeSide.Long ? p.EntryPrice + options.Stop3EntryOffset : p.EntryPrice - options.Stop3EntryOffset, options.TickSize);
                    p.Stage = p.RemainingQuantity <= 0m ? Bot8011LifecycleStage.Closed : Bot8011LifecycleStage.Stop3Active;
                    if (p.Stage == Bot8011LifecycleStage.Closed) FinalizePosition(p, p.TakeProfit, candle.CloseTimeUtc, "TP_FULL_EXIT");
                }
            }
            else if (p.Stage == Bot8011LifecycleStage.Stop3Active && p.Stop3Current.HasValue)
            {
                var hit = p.Side == TradeSide.Long ? candle.Low <= p.Stop3Current : candle.High >= p.Stop3Current;
                if (hit) { CloseRemaining(p, ApplyExitSlippage(p.Stop3Current.Value, p.Side), candle.CloseTimeUtc, "STOP3"); active = null; }
            }
        }

        void UpdateTrailing(Bot8011SimulatedPosition p, decimal close)
        {
            if (!p.Stop3Current.HasValue) return;
            if (p.Side == TradeSide.Long)
            {
                var candidate = Round(close - options.Stop3TrailingBuffer, options.TickSize);
                if (candidate >= p.Stop3Current.Value + options.Stop3TrailingStep) p.Stop3Current = candidate;
            }
            else
            {
                var candidate = Round(close + options.Stop3TrailingBuffer, options.TickSize);
                if (candidate <= p.Stop3Current.Value - options.Stop3TrailingStep) p.Stop3Current = candidate;
            }
        }

        void CloseRemaining(Bot8011SimulatedPosition p, decimal price, DateTime time, string reason)
        {
            if (p.RemainingQuantity > 0m) Realize(p, price, p.RemainingQuantity, time, reason);
            p.Stage = Bot8011LifecycleStage.Closed;
            FinalizePosition(p, price, time, reason);
        }

        void Realize(Bot8011SimulatedPosition p, decimal rawPrice, decimal quantity, DateTime time, string reason)
        {
            var price = Round(rawPrice, options.TickSize);
            var gross = p.Side == TradeSide.Long ? (price - p.EntryPrice) * quantity : (p.EntryPrice - price) * quantity;
            var fee = price * quantity * options.TakerFeeRate;
            p.RealizedGrossPnl += gross; p.Fees += fee; p.RemainingQuantity = Math.Max(0m, p.RemainingQuantity - quantity);
            balance += gross - fee;
            executions.Add(new(p.Id, time, "EXIT", p.Side, price, quantity, gross, fee, reason));
        }

        void FinalizePosition(Bot8011SimulatedPosition p, decimal exitPrice, DateTime exitTime, string reason)
        {
            if (positions.Any(x => x.PositionId == p.Id)) return;
            positions.Add(new Bot8011PositionResult
            {
                PositionId = p.Id,
                Side = p.Side,
                EntryTimeUtc = p.EntryTimeUtc,
                EntryPrice = p.EntryPrice,
                ExitTimeUtc = exitTime,
                ExitPrice = exitPrice,
                Quantity = p.InitialQuantity,
                GrossPnl = p.RealizedGrossPnl,
                Fees = p.Fees,
                NetPnl = p.RealizedGrossPnl - p.Fees,
                ExitReason = reason,
                TpExecuted = p.TpExecuted
            });
        }

        decimal ApplyEntrySlippage(decimal price, TradeSide side) => side == TradeSide.Long
            ? price * (1m + options.SlippageBasisPoints / 10_000m) : price * (1m - options.SlippageBasisPoints / 10_000m);
        decimal ApplyExitSlippage(decimal price, TradeSide side) => side == TradeSide.Long
            ? price * (1m - options.SlippageBasisPoints / 10_000m) : price * (1m + options.SlippageBasisPoints / 10_000m);
    }

    private static Bot8011Metrics CalculateMetrics(decimal initial, decimal final, IReadOnlyList<Bot8011PositionResult> positions, IReadOnlyList<Bot8011EquityPoint> equity)
    {
        var wins = positions.Where(x => x.NetPnl > 0m).ToArray();
        var losses = positions.Where(x => x.NetPnl <= 0m).ToArray();
        var gp = wins.Sum(x => x.NetPnl); var gl = Math.Abs(losses.Sum(x => x.NetPnl));
        return new Bot8011Metrics
        {
            Positions = positions.Count,
            Wins = wins.Length,
            Losses = losses.Length,
            WinRatePercent = positions.Count == 0 ? 0m : wins.Length * 100m / positions.Count,
            InitialBalance = initial,
            FinalBalance = final,
            NetProfit = final - initial,
            ReturnPercent = initial == 0m ? 0m : (final - initial) / initial * 100m,
            GrossProfit = gp,
            GrossLoss = gl,
            ProfitFactor = gl == 0m ? (gp > 0m ? 999m : 0m) : gp / gl,
            MaximumDrawdown = equity.Count == 0 ? 0m : equity.Max(x => x.Drawdown),
            MaximumDrawdownPercent = equity.Count == 0 ? 0m : equity.Max(x => x.DrawdownPercent),
            TotalFees = positions.Sum(x => x.Fees),
            Expectancy = positions.Count == 0 ? 0m : positions.Average(x => x.NetPnl),
            TpReachedCount = positions.Count(x => x.TpExecuted)
        };
    }

    private static decimal Round(decimal value, decimal step) => step <= 0m ? value : Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
    private static decimal RoundDown(decimal value, decimal step) => step <= 0m ? value : Math.Floor(value / step) * step;
    private static void Validate(Bot8011BacktestOptions o, IReadOnlyList<HistoricalCandle> candles)
    {
        if (candles.Count < 2) throw new InvalidOperationException("At least two candles are required.");
        if (o.Quantity <= 0m || o.Leverage <= 0 || o.TakeProfitPercent <= 0m || o.StopLossPercent <= 0m) throw new ArgumentException("Invalid BOT8011 options.");
        if (o.TakeProfitCloseFraction <= 0m || o.TakeProfitCloseFraction > 1m) throw new ArgumentException("TakeProfitCloseFraction must be in (0,1].");
    }
    private sealed record PendingSignal(Bot8011Signal Signal);
}
