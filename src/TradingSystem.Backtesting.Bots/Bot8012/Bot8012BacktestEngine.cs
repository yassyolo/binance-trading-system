using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Models.Enums;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Bot8012;

public sealed class Bot8012BacktestEngine
{
    public BotBacktestResult<Bot8012BacktestOptions> Run(
        IReadOnlyList<MarketCandle> sourceCandles,
        IReadOnlyList<HistoricalBotSignal> sourceSignals,
        Bot8012BacktestOptions options,
        CancellationToken cancellationToken = default)
    {
        Validate(sourceCandles, options);
        var started = DateTime.UtcNow;
        var candles = sourceCandles.OrderBy(x => x.OpenTimeUtc).ToArray();
        var signals = sourceSignals.OrderBy(x => x.TimeUtc).ToArray();
        var active = new List<Position>();
        var completed = new List<BotPositionResult>();
        var executions = new List<BotExecution>();
        var decisions = new List<BotSignalDecision>();
        var equity = new List<BotEquityPoint>();
        var pending = new Queue<HistoricalBotSignal>();
        var lastSignalAt = new Dictionary<TradeSide, DateTime>();
        var balance = options.InitialBalance;
        var peak = balance;
        var signalIndex = 0;

        foreach (var candle in candles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (signalIndex < signals.Length && signals[signalIndex].TimeUtc <= candle.OpenTimeUtc)
                pending.Enqueue(signals[signalIndex++]);

            while (pending.TryDequeue(out var signal))
            {
                var enabled = signal.Side == TradeSide.Long ? options.EnableLong : options.EnableShort;
                if (!enabled)
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "SIDE_DISABLED", candle.Open, signal.SignalId));
                    continue;
                }

                if (lastSignalAt.TryGetValue(signal.Side, out var last)
                    && signal.TimeUtc - last < TimeSpan.FromSeconds(options.CooldownSeconds))
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "SIGNAL_COOLDOWN", candle.Open, signal.SignalId));
                    continue;
                }

                var policy = ValidateGap(signal.Side, candle.Open, active, options);
                if (!policy.Open)
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", policy.Reason, candle.Open, signal.SignalId));
                    continue;
                }

                var entry = BotBacktestMath.RoundToStep(
                    BotBacktestMath.EntrySlippage(candle.Open, signal.Side, options.SlippageBasisPoints),
                    options.TickSize);
                var margin = entry * options.Quantity / options.Leverage;
                var usedMargin = active.Sum(x => x.Margin);
                var entryFee = entry * options.Quantity * options.EntryFeeRate;
                var availableBalance = balance - usedMargin;

                if (margin + entryFee > availableBalance)
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "INSUFFICIENT_FREE_MARGIN", entry, signal.SignalId));
                    continue;
                }

                var takeProfit = BotBacktestMath.RoundToStep(
                    signal.Side == TradeSide.Long
                        ? entry + options.ProfitDistance
                        : entry - options.ProfitDistance,
                    options.TickSize);

                balance -= entryFee;
                var position = new Position(
                    Guid.NewGuid().ToString("N")[..8],
                    signal.Side,
                    signal.TimeUtc,
                    candle.OpenTimeUtc,
                    entry,
                    takeProfit,
                    options.Quantity,
                    margin,
                    entryFee);
                active.Add(position);
                executions.Add(new(position.Id, candle.OpenTimeUtc, "ENTRY", position.Side, entry, position.Quantity, 0, entryFee, signal.Source));
                decisions.Add(new(signal.TimeUtc, signal.Side, "Open", policy.Reason, entry, signal.SignalId));
                lastSignalAt[signal.Side] = signal.TimeUtc;
            }

            foreach (var position in active.ToArray())
            {
                var hit = position.Side == TradeSide.Long
                    ? candle.High >= position.TakeProfit
                    : candle.Low <= position.TakeProfit;
                if (hit)
                    Close(position, position.TakeProfit, candle.CloseTimeUtc, "TAKE_PROFIT");
            }

            var unrealized = active.Sum(x => BotBacktestMath.UnrealizedPnl(x.Side, x.EntryPrice, candle.Close, x.Quantity));
            var totalEquity = balance + unrealized;
            peak = Math.Max(peak, totalEquity);
            var drawdown = peak - totalEquity;
            equity.Add(new(candle.CloseTimeUtc, balance, totalEquity, peak, drawdown, peak == 0 ? 0 : drawdown / peak * 100m));
        }

        if (options.ForceCloseAtEnd)
        {
            var last = candles[^1];
            foreach (var position in active.ToArray())
                Close(position, BotBacktestMath.ExitSlippage(last.Close, position.Side, options.SlippageBasisPoints), last.CloseTimeUtc, "BACKTEST_END");
        }

        return new BotBacktestResult<Bot8012BacktestOptions>
        {
            RunId = $"BOT8012_{options.Symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}",
            BotName = options.BotName,
            Options = options,
            StartedAtUtc = started,
            CompletedAtUtc = DateTime.UtcNow,
            Metrics = BotBacktestMath.Metrics(signals.Length, decisions.Count(x => x.Decision == "Block"), options.InitialBalance, balance, completed, equity),
            Positions = completed,
            Executions = executions,
            Decisions = decisions,
            EquityCurve = equity,
            Candles = candles,
            Signals = signals
        };

        void Close(Position position, decimal rawPrice, DateTime time, string reason)
        {
            var price = BotBacktestMath.RoundToStep(rawPrice, options.TickSize);
            var gross = BotBacktestMath.UnrealizedPnl(position.Side, position.EntryPrice, price, position.Quantity);
            var exitFee = price * position.Quantity * options.ExitFeeRate;
            balance += gross - exitFee;
            active.Remove(position);
            executions.Add(new(position.Id, time, "EXIT", position.Side, price, position.Quantity, gross, exitFee, reason));
            completed.Add(new BotPositionResult
            {
                PositionId = position.Id,
                Side = position.Side,
                EntryTimeUtc = position.EntryTimeUtc,
                EntryPrice = position.EntryPrice,
                ExitTimeUtc = time,
                ExitPrice = price,
                Quantity = position.Quantity,
                GrossPnl = gross,
                Fees = position.EntryFee + exitFee,
                NetPnl = gross - position.EntryFee - exitFee,
                ExitReason = reason,
                PartialTakeProfitReached = false
            });
        }
    }

    private static (bool Open, string Reason) ValidateGap(
        TradeSide side,
        decimal markPrice,
        IReadOnlyCollection<Position> active,
        Bot8012BacktestOptions options)
    {
        var sameSide = active
            .Where(x => x.Side == side)
            .OrderByDescending(x => x.EntryTimeUtc)
            .ToArray();

        if (sameSide.Length >= options.OrderSideLimit)
            return (false, $"ORDER_SIDE_LIMIT reached ({sameSide.Length}/{options.OrderSideLimit})");
        if (sameSide.Length == 0)
            return (true, "No active TP positions for this side");

        var newestTakeProfit = sameSide[0].TakeProfit;
        if (side == TradeSide.Long)
        {
            var maximum = newestTakeProfit - options.ProfitDistance - options.PriceDistance;
            return markPrice <= maximum
                ? (true, $"LONG spacing valid: mark={markPrice}, maximum={maximum}")
                : (false, $"GAP fail LONG: mark={markPrice}, maximum={maximum}");
        }

        var minimum = newestTakeProfit + options.ProfitDistance + options.PriceDistance;
        return markPrice >= minimum
            ? (true, $"SHORT spacing valid: mark={markPrice}, minimum={minimum}")
            : (false, $"GAP fail SHORT: mark={markPrice}, minimum={minimum}");
    }

    private static void Validate(IReadOnlyList<MarketCandle> candles, Bot8012BacktestOptions options)
    {
        if (candles.Count < 2)
            throw new InvalidOperationException("At least two candles are required.");
        if (options.InitialBalance <= 0
            || options.Quantity <= 0
            || options.Leverage <= 0
            || options.PriceDistance <= 0
            || options.ProfitDistance <= 0
            || options.OrderSideLimit <= 0
            || options.TickSize <= 0
            || options.EntryFeeRate < 0
            || options.ExitFeeRate < 0
            || options.SlippageBasisPoints < 0)
            throw new ArgumentException("Invalid BOT8012 options.");
    }

    private sealed record Position(
        string Id,
        TradeSide Side,
        DateTime SignalTimeUtc,
        DateTime EntryTimeUtc,
        decimal EntryPrice,
        decimal TakeProfit,
        decimal Quantity,
        decimal Margin,
        decimal EntryFee);
}
