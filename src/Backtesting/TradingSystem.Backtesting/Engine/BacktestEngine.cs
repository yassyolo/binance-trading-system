using TradingSystem.Backtesting.Costs;
using TradingSystem.Backtesting.Execution;
using TradingSystem.Backtesting.Metrics;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Portfolio;
using TradingSystem.Backtesting.Risk;
using TradingSystem.Backtesting.Strategies;

namespace TradingSystem.Backtesting.Engine;

public sealed class BacktestEngine(
    BacktestStrategyRegistry registry,
    PositionSizeCalculator sizeCalculator,
    CandleFillResolver fillResolver,
    BacktestMetricsCalculator metricsCalculator,
    ITradingCostModel costModel)
{
    public async Task<BacktestResult> RunAsync(BacktestRequest request, IReadOnlyList<HistoricalCandle> sourceCandles,
        SymbolTradingRules rules, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        var candles = sourceCandles
            .Where(x => (!request.StartUtc.HasValue || x.OpenTimeUtc >= request.StartUtc.Value) &&
                        (!request.EndUtc.HasValue || x.OpenTimeUtc <= request.EndUtc.Value))
            .OrderBy(x => x.OpenTimeUtc).ToArray();
        Validate(candles);

        var strategy = registry.Create(request.StrategyName, request.StrategyParameters);
        var portfolio = new BacktestPortfolio(request.InitialBalance, rules, costModel);
        var signals = new List<SignalMarker>();
        PendingEntry? pending = null;

        for (var i = 0; i < candles.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candle = candles[i];

            if (pending is not null)
            {
                Open(pending, candle.Open, candle.OpenTimeUtc);
                pending = null;
            }

            if (portfolio.ActivePosition is not null)
            {
                var fill = fillResolver.Resolve(portfolio.ActivePosition, candle, request.ConflictPolicy);
                if (fill is not null)
                {
                    var price = costModel.ApplyExitSlippage(fill.Price, portfolio.ActivePosition.Side, request.SlippageBasisPoints);
                    portfolio.Close(Round(price, rules.TickSize), candle.CloseTimeUtc, fill.Reason);
                }
            }

            if (i < strategy.WarmupBars) continue;
            var context = new BacktestStrategyContext
            {
                CurrentCandle = candle,
                PreviousCandle = i > 0 ? candles[i - 1] : null,
                History = new ArraySegment<HistoricalCandle>(candles, 0, i + 1),
                ActivePosition = portfolio.ActivePosition,
                Balance = portfolio.Balance,
                BarIndex = i
            };
            var decision = await strategy.DecideAsync(context, cancellationToken);
            if (decision.Type == DecisionType.None) continue;

            if ((decision.Type is DecisionType.Close or DecisionType.CloseAndReverse) && portfolio.ActivePosition is not null)
            {
                var closePrice = costModel.ApplyExitSlippage(candle.Close, portfolio.ActivePosition.Side, request.SlippageBasisPoints);
                portfolio.Close(Round(closePrice, rules.TickSize), candle.CloseTimeUtc,
                    decision.Type == DecisionType.CloseAndReverse ? ExitReason.ReverseSignal : ExitReason.Strategy);
            }

            if (decision.Type is DecisionType.Open or DecisionType.CloseAndReverse)
            {
                if (!decision.Side.HasValue || !decision.StopLoss.HasValue) continue;
                var marker = new SignalMarker(candle.CloseTimeUtc, decision.Side.Value, candle.Close, false, decision.Reason);
                if (request.EntryExecutionMode == EntryExecutionMode.SignalCandleClose)
                {
                    var success = Open(new PendingEntry(decision.Side.Value, decision.StopLoss.Value, decision.TakeProfit, marker), candle.Close, candle.CloseTimeUtc);
                    signals.Add(marker with { Executed = success });
                }
                else
                {
                    pending = new PendingEntry(decision.Side.Value, decision.StopLoss.Value, decision.TakeProfit, marker);
                    signals.Add(marker with { Executed = true, Reason = decision.Reason + " (pending next open)" });
                }
            }
        }

        if (portfolio.ActivePosition is not null)
        {
            var last = candles[^1];
            var price = costModel.ApplyExitSlippage(last.Close, portfolio.ActivePosition.Side, request.SlippageBasisPoints);
            portfolio.Close(Round(price, rules.TickSize), last.CloseTimeUtc, ExitReason.BacktestEnd);
        }

        var metrics = metricsCalculator.Calculate(portfolio.InitialBalance, portfolio.Balance, portfolio.Trades, portfolio.EquityCurve);
        return new BacktestResult
        {
            RunId = $"{request.StrategyName}_{request.Symbol}_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
            Request = request,
            StartedAtUtc = started,
            CompletedAtUtc = DateTime.UtcNow,
            InitialBalance = portfolio.InitialBalance,
            FinalBalance = portfolio.Balance,
            Metrics = metrics,
            Trades = portfolio.Trades.ToArray(),
            EquityCurve = portfolio.EquityCurve.ToArray(),
            Candles = candles,
            Signals = signals
        };

        bool Open(PendingEntry entry, decimal rawPrice, DateTime timeUtc)
        {
            if (portfolio.ActivePosition is not null) return false;
            var price = Round(costModel.ApplyEntrySlippage(rawPrice, entry.Side, request.SlippageBasisPoints), rules.TickSize);
            var sl = Round(entry.StopLoss, rules.TickSize);
            decimal? tp = entry.TakeProfit.HasValue ? Round(entry.TakeProfit.Value, rules.TickSize) : (decimal?)null;
            var size = sizeCalculator.Calculate(portfolio.Balance, request.RiskPerTradePercent, price, sl, rules);
            if (!size.Succeeded) return false;
            var risk = Math.Abs(price - sl) * size.Quantity * rules.ContractMultiplier;
            portfolio.Open(request.Symbol, entry.Side, price, size.Quantity, sl, tp, timeUtc, risk);
            return true;
        }
    }

    private static decimal Round(decimal value, decimal step)
        => step <= 0m ? value : Math.Round(value / step, MidpointRounding.AwayFromZero) * step;

    private static void Validate(IReadOnlyList<HistoricalCandle> candles)
    {
        if (candles.Count < 2) throw new InvalidOperationException("At least two candles are required.");
        for (var i = 1; i < candles.Count; i++)
        {
            if (candles[i].OpenTimeUtc <= candles[i - 1].OpenTimeUtc)
                throw new InvalidOperationException("Candles must have unique ascending timestamps.");
            if (candles[i].Low > candles[i].High || candles[i].Open < candles[i].Low || candles[i].Open > candles[i].High ||
                candles[i].Close < candles[i].Low || candles[i].Close > candles[i].High)
                throw new InvalidOperationException($"Invalid OHLC candle at {candles[i].OpenTimeUtc:O}.");
        }
    }

    private sealed record PendingEntry(TradeSide Side, decimal StopLoss, decimal? TakeProfit, SignalMarker Marker);
}
