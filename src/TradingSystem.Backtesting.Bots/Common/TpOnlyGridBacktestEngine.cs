using TradingSystem.Backtesting.Models;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.MarketData;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Grid.Models;

namespace TradingSystem.Backtesting.Bots.Common;

public interface ITpOnlyGridBacktestOptions
{
    string BotName { get; }
    string Symbol { get; }
    decimal InitialBalance { get; }
    decimal Quantity { get; }
    int Leverage { get; }
    decimal PriceDistance { get; }
    decimal ProfitDistance { get; }
    int OrderSideLimit { get; }
    int CooldownSeconds { get; }
    bool EnableLong { get; }
    bool EnableShort { get; }
    decimal EntryFeeRate { get; }
    decimal ExitFeeRate { get; }
    decimal SlippageBasisPoints { get; }
    decimal TickSize { get; }
    bool ForceCloseAtEnd { get; }
}

public sealed class TpOnlyGridBacktestEngine(GridSpacingPolicy policy)
{
    public BotBacktestResult<TOptions> Run<TOptions>(
        IReadOnlyList<MarketCandle> sourceCandles,
        IReadOnlyList<HistoricalBotSignal> sourceSignals,
        TOptions options,
        CancellationToken cancellationToken = default)
        where TOptions : ITpOnlyGridBacktestOptions
    {
        Validate(sourceCandles, options);
        var candles = sourceCandles.OrderBy(x => x.OpenTimeUtc).ToArray();
        var signals = sourceSignals.OrderBy(x => x.TimeUtc).ToArray();
        var active = new List<Position>();
        var completed = new List<BotPositionResult>();
        var executions = new List<BotExecution>();
        var decisions = new List<BotSignalDecision>();
        var equity = new List<BotEquityPoint>();
        var lastSignalAt = new Dictionary<TradeSide, DateTime>();
        var balance = options.InitialBalance;
        var peak = balance;
        var signalIndex = 0;

        foreach (var candle in candles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (signalIndex < signals.Length && signals[signalIndex].TimeUtc <= candle.OpenTimeUtc)
            {
                var signal = signals[signalIndex++];
                var positionSide = ToPositionSide(signal.Side);

                if (signal.Side == TradeSide.Long && !options.EnableLong
                    || signal.Side == TradeSide.Short && !options.EnableShort)
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "SIDE_DISABLED", candle.Open, signal.SignalId));
                    continue;
                }

                if (lastSignalAt.TryGetValue(signal.Side, out var previous)
                    && signal.TimeUtc - previous < TimeSpan.FromSeconds(options.CooldownSeconds))
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", "SIGNAL_COOLDOWN", candle.Open, signal.SignalId));
                    continue;
                }

                var references = active
                    .Select(x => new GridPositionReference(ToPositionSide(x.Side), x.TakeProfit, x.EntryTimeUtc))
                    .ToArray();
                var check = policy.Evaluate(
                    positionSide,
                    candle.Open,
                    references,
                    new(options.PriceDistance, options.ProfitDistance, options.OrderSideLimit));

                if (!check.Allowed)
                {
                    decisions.Add(new(signal.TimeUtc, signal.Side, "Block", check.Reason, candle.Open, signal.SignalId));
                    continue;
                }

                var entry = BotBacktestMath.RoundToStep(
                    BotBacktestMath.EntrySlippage(candle.Open, signal.Side, options.SlippageBasisPoints),
                    options.TickSize);
                var margin = entry * options.Quantity / options.Leverage;
                var usedMargin = active.Sum(x => x.Margin);
                var entryFee = entry * options.Quantity * options.EntryFeeRate;

                if (margin + entryFee > balance - usedMargin)
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
                    candle.OpenTimeUtc,
                    entry,
                    takeProfit,
                    options.Quantity,
                    margin,
                    entryFee);
                active.Add(position);
                executions.Add(new(position.Id, candle.OpenTimeUtc, "ENTRY", position.Side, entry, position.Quantity, 0, entryFee, signal.Source));
                decisions.Add(new(signal.TimeUtc, signal.Side, "Open", check.Reason, entry, signal.SignalId));
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
            var currentEquity = balance + unrealized;
            peak = Math.Max(peak, currentEquity);
            var drawdown = peak - currentEquity;
            equity.Add(new(candle.CloseTimeUtc, balance, currentEquity, peak, drawdown, peak == 0 ? 0 : drawdown / peak * 100m));
        }

        if (options.ForceCloseAtEnd)
        {
            var last = candles[^1];
            foreach (var position in active.ToArray())
                Close(position, BotBacktestMath.ExitSlippage(last.Close, position.Side, options.SlippageBasisPoints), last.CloseTimeUtc, "BACKTEST_END");
        }

        return new BotBacktestResult<TOptions>
        {
            RunId = $"{options.BotName}_{options.Symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}",
            BotName = options.BotName,
            Options = options,
            StartedAtUtc = candles[0].OpenTimeUtc,
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

    private static void Validate<TOptions>(IReadOnlyList<MarketCandle> candles, TOptions options)
        where TOptions : ITpOnlyGridBacktestOptions
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
            throw new ArgumentException("Invalid TP-only grid backtest options.");
    }

    private static PositionSide ToPositionSide(TradeSide side)
        => side == TradeSide.Long ? PositionSide.Long : PositionSide.Short;

    private sealed record Position(
        string Id,
        TradeSide Side,
        DateTime EntryTimeUtc,
        decimal EntryPrice,
        decimal TakeProfit,
        decimal Quantity,
        decimal Margin,
        decimal EntryFee);
}
