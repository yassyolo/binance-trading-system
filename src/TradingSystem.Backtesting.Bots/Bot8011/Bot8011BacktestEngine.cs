using TradingSystem.Backtesting.Bots.Bot8011.Models;
using TradingSystem.Backtesting.Bots.Bot8011.Models.Enums;
using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Models.Enums;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Bot8011;

public sealed class Bot8011BacktestEngine
{
    public Task<BotBacktestResult<Bot8011BacktestOptions>> RunAsync(
        decimal initialBalance,
        Bot8011BacktestOptions options,
        IReadOnlyList<MarketCandle> sourceCandles,
        IReadOnlyList<HistoricalBotSignal> sourceSignals,
        CancellationToken cancellationToken = default)
    {
        Validate(initialBalance, options, sourceCandles);
        
        var started = DateTime.UtcNow;
        var candles = sourceCandles.OrderBy(x => x.OpenTimeUtc).ToArray();
        var signals = sourceSignals.OrderBy(x => x.TimeUtc).ToArray();
        var executions = new List<BotExecution>();
        var positions = new List<BotPositionResult>();
        var decisions = new List<BotSignalDecision>();
        var equity = new List<BotEquityPoint>();
        var lastSignalAt = new Dictionary<TradeSide, DateTime>();
        var balance = initialBalance;
        var peak = balance;
        var signalIndex = 0;
        SimulatedPosition? active = null;
        HistoricalBotSignal? pending = null;

        foreach (var candle in candles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (pending is not null)
            {
                active ??= TryOpen(pending, candle.Open, candle.OpenTimeUtc);
                pending = null;
            }

            if (active is not null)
            {
                ProcessProtection(active, candle);

                if (active is not null && active.Stage == LifecycleStage.Stop3Active)
                    UpdateTrailing(active, candle.Close);
            }

            while (signalIndex < signals.Length && signals[signalIndex].TimeUtc <= candle.CloseTimeUtc)
            {
                var signal = signals[signalIndex++];
                if (signal.TimeUtc < candle.OpenTimeUtc)
                    continue;

                var sideEnabled = signal.Side == TradeSide.Long ? options.EnableLong : options.EnableShort;
                if (!sideEnabled)
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "SIDE_DISABLED", candle.Close, signal.SignalId));
                    continue;
                }

                if (lastSignalAt.TryGetValue(signal.Side, out var last) &&
                    signal.TimeUtc - last < TimeSpan.FromSeconds(options.CooldownSeconds))
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "SIGNAL_COOLDOWN", candle.Close, signal.SignalId));
                    continue;
                }

                if (active is not null && active.Side != signal.Side)
                {
                    CloseRemaining(active!, BotBacktestMath.ExitSlippage(candle.Close, active!.Side, options.SlippageBasisPoints), candle.CloseTimeUtc, "REVERSE_SIGNAL");
                    active = null;
                }
                else if (active is not null)
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "ORDER_SIDE_LIMIT reached (1/1)", candle.Close, signal.SignalId));
                    continue;
                }

                lastSignalAt[signal.Side] = signal.TimeUtc;
                decisions.Add(new(signal.TimeUtc, signal.Side, "Open", options.EnterOnNextCandleOpen ? "PENDING_NEXT_OPEN" : "SIGNAL_CLOSE", candle.Close, signal.SignalId));
                if (options.EnterOnNextCandleOpen)
                    pending = signal;
                else
                    active = TryOpen(signal, candle.Close, candle.CloseTimeUtc);
            }

            var unrealized = active is null ? 0 : BotBacktestMath.UnrealizedPnl(active.Side, active.EntryPrice, candle.Close, active.RemainingQuantity);
            var totalEquity = balance + unrealized;
            peak = Math.Max(peak, totalEquity);
            var drawdown = peak - totalEquity;
            equity.Add(new(candle.CloseTimeUtc, balance, totalEquity, peak, drawdown, peak == 0 ? 0 : drawdown / peak * 100m));
        }

        if (active is not null)
        {
            var last = candles[^1];
            CloseRemaining(active, BotBacktestMath.ExitSlippage(last.Close, active.Side, options.SlippageBasisPoints), last.CloseTimeUtc, "BACKTEST_END");
        }

        return Task.FromResult(new BotBacktestResult<Bot8011BacktestOptions>
        {
            RunId = $"BOT8011_{options.Symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}",
            BotName = options.BotName,
            Options = options,
            StartedAtUtc = started,
            CompletedAtUtc = DateTime.UtcNow,
            Metrics = BotBacktestMath.Metrics(signals.Length, decisions.Count(x => x.Decision == "Block"), initialBalance, balance, positions, equity),
            Positions = positions,
            Executions = executions,
            Decisions = decisions,
            EquityCurve = equity,
            Candles = candles,
            Signals = signals
        });

        SimulatedPosition? TryOpen(HistoricalBotSignal signal, decimal rawPrice, DateTime time)
        {
            var quantity = BotBacktestMath.RoundDown(options.Quantity, options.QuantityStep);
            var price = BotBacktestMath.RoundToStep(BotBacktestMath.EntrySlippage(rawPrice, signal.Side, options.SlippageBasisPoints), options.TickSize);
            if (quantity < options.MinimumQuantity || price * quantity < options.MinimumNotional || price * quantity / options.Leverage > balance)
            {
                decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "QUANTITY_OR_MARGIN_INVALID", price, signal.SignalId));
                return null;
            }

            var sl = signal.Side == TradeSide.Long
                ? price - options.InitialStopLossDistance
                : price + options.InitialStopLossDistance;
            var tp = signal.Side == TradeSide.Long
                ? price * (1 + options.TakeProfitPercent / 100m)
                : price * (1 - options.TakeProfitPercent / 100m);
            var fee = price * quantity * options.TakerFeeRate;
            balance -= fee;
            var position = new SimulatedPosition
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Side = signal.Side,
                EntryTimeUtc = time,
                EntryPrice = price,
                InitialQuantity = quantity,
                RemainingQuantity = quantity,
                InitialStopLoss = BotBacktestMath.RoundToStep(sl, options.TickSize),
                TakeProfit = BotBacktestMath.RoundToStep(tp, options.TickSize),
                Fees = fee
            };
            executions.Add(new(position.Id, time, "ENTRY", position.Side, price, quantity, 0, fee, signal.Source));
            return position;
        }

        void ProcessProtection(SimulatedPosition position, MarketCandle candle)
        {
            if (position.Stage == LifecycleStage.InitialProtection)
            {
                var slHit = position.Side == TradeSide.Long ? candle.Low <= position.InitialStopLoss : candle.High >= position.InitialStopLoss;
                var tpHit = position.Side == TradeSide.Long ? candle.High >= position.TakeProfit : candle.Low <= position.TakeProfit;
                if (slHit && tpHit)
                {
                    if (options.ConflictPolicy is IntrabarConflictPolicy.WorstCase or IntrabarConflictPolicy.StopLossFirst) tpHit = false;
                    else slHit = false;
                }
                if (slHit)
                {
                    CloseRemaining(position, BotBacktestMath.ExitSlippage(position.InitialStopLoss, position.Side, options.SlippageBasisPoints), candle.CloseTimeUtc, "INITIAL_SL");
                    active = null;
                    return;
                }
                if (tpHit)
                {
                    var closeQty = Math.Min(position.RemainingQuantity, BotBacktestMath.RoundDown(position.InitialQuantity * options.TakeProfitCloseFraction, options.QuantityStep));
                    Realize(position, position.TakeProfit, closeQty, candle.CloseTimeUtc, "TP_PARTIAL");
                    position.PartialTpReached = true;
                    position.Stop3Current = BotBacktestMath.RoundToStep(
                        position.Side == TradeSide.Long ? position.EntryPrice + options.Stop3EntryOffset : position.EntryPrice - options.Stop3EntryOffset,
                        options.TickSize);
                    position.Stage = position.RemainingQuantity <= 0 ? LifecycleStage.Closed : LifecycleStage.Stop3Active;
                    if (position.Stage == LifecycleStage.Closed)
                        Finalize(position, position.TakeProfit, candle.CloseTimeUtc, "TP_FULL_EXIT");
                }
            }
            else if (position.Stage == LifecycleStage.Stop3Active && position.Stop3Current.HasValue)
            {
                var hit = position.Side == TradeSide.Long ? candle.Low <= position.Stop3Current : candle.High >= position.Stop3Current;
                if (hit)
                {
                    CloseRemaining(position, BotBacktestMath.ExitSlippage(position.Stop3Current.Value, position.Side, options.SlippageBasisPoints), candle.CloseTimeUtc, "STOP3");
                    active = null;
                }
            }
        }

        void UpdateTrailing(SimulatedPosition position, decimal close)
        {
            if (!position.Stop3Current.HasValue) 
                return;
           
            if (position.Side == TradeSide.Long 
                && close >= position.Stop3Current.Value + options.Stop3TrailingStep)
                position.Stop3Current = BotBacktestMath.RoundToStep(position.Stop3Current.Value + options.Stop3TrailingBuffer, options.TickSize);
            else if (position.Side == TradeSide.Short
                && close <= position.Stop3Current.Value - options.Stop3TrailingStep)
                position.Stop3Current = BotBacktestMath.RoundToStep(position.Stop3Current.Value - options.Stop3TrailingBuffer, options.TickSize);
        }

        void CloseRemaining(SimulatedPosition position, decimal price, DateTime time, string reason)
        {
            if (position.RemainingQuantity > 0) 
                Realize(position, price, position.RemainingQuantity, time, reason);
            
            position.Stage = LifecycleStage.Closed;
            
            Finalize(position, price, time, reason);
        }

        void Realize(SimulatedPosition position, decimal rawPrice, decimal quantity, DateTime time, string reason)
        {
            var price = BotBacktestMath.RoundToStep(rawPrice, options.TickSize);
            var gross = BotBacktestMath.UnrealizedPnl(position.Side, position.EntryPrice, price, quantity);
            var fee = price * quantity * options.TakerFeeRate;
           
            position.RealizedGross += gross;
            position.Fees += fee;
            position.RemainingQuantity = Math.Max(0, position.RemainingQuantity - quantity);
            balance += gross - fee;
            
            executions.Add(new(position.Id, time, "EXIT", position.Side, price, quantity, gross, fee, reason));
        }

        void Finalize(SimulatedPosition position, decimal exitPrice, DateTime exitTime, string reason)
        {
            if (positions.Any(x => x.PositionId == position.Id)) return;
            positions.Add(new BotPositionResult
            {
                PositionId = position.Id,
                Side = position.Side,
                EntryTimeUtc = position.EntryTimeUtc,
                EntryPrice = position.EntryPrice,
                ExitTimeUtc = exitTime,
                ExitPrice = exitPrice,
                Quantity = position.InitialQuantity,
                GrossPnl = position.RealizedGross,
                Fees = position.Fees,
                NetPnl = position.RealizedGross - position.Fees,
                ExitReason = reason,
                PartialTakeProfitReached = position.PartialTpReached
            });
        }
    }

    private static void Validate(decimal initialBalance, Bot8011BacktestOptions options, IReadOnlyList<MarketCandle> candles)
    {
        if (initialBalance <= 0) throw new ArgumentOutOfRangeException(nameof(initialBalance));
        if (candles.Count < 2) throw new InvalidOperationException("At least two candles are required.");
        if (options.Quantity <= 0 || options.Leverage <= 0 || options.TakeProfitPercent <= 0 || options.InitialStopLossDistance <= 0)
            throw new ArgumentException("Invalid BOT8011 options.");
        if (options.TakeProfitCloseFraction is <= 0 or > 1)
            throw new ArgumentException("TakeProfitCloseFraction must be in (0,  1].");
    }    
}
