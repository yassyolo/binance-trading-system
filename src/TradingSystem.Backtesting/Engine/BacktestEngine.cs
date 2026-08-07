using TradingSystem.Backtesting.Costs.Contracts;
using TradingSystem.Backtesting.Execution;
using TradingSystem.Backtesting.Metrics;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Portfolio;
using TradingSystem.Backtesting.Risk;
using TradingSystem.Backtesting.Strategies;
using TradingSystem.Backtesting.Strategies.Models;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Engine;

public sealed class BacktestEngine(
    BacktestStrategyRegistry registry,
    PositionSizeCalculator sizeCalculator,
    CandleFillResolver fillResolver,
    BacktestMetricsCalculator metricsCalculator,
    ITradingCostModel costModel)
{
    public async Task<BacktestResult> RunAsync(BacktestRequest request, IReadOnlyList<MarketCandle> sourceCandles, SymbolTradingRules rules, CancellationToken ct = default)
    {
        BacktestRequestValidator.Validate(request, rules);
       
        var started = DateTime.UtcNow;
        
        var candles = sourceCandles
            .Where(x => (!request.StartUtc.HasValue || x.OpenTimeUtc >= request.StartUtc.Value)
                        && (!request.EndUtc.HasValue || x.OpenTimeUtc < request.EndUtc.Value))
            .OrderBy(x => x.OpenTimeUtc)
            .ToArray();     
        Validate(candles);

        var strategy = registry.Create(request.StrategyName, request.StrategyParameters);
        
        var portfolio = new BacktestPortfolio(request.InitialBalance, rules, costModel);
       
        var signals = new List<SignalMarker>();
       
        PendingEntry? pending = null;

        for (var i = 0; i < candles.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            var candle = candles[i];

            if (pending is not null)
            {
                var opened = Open(pending, candle.Open, candle.OpenTimeUtc);
                signals[pending.MarkerIndex] = signals[pending.MarkerIndex] with
                {
                    Executed = opened,
                    Reason = opened
                        ? pending.Reason + " (executed at next open)"
                        : pending.Reason + " (rejected at next open)"
                };
                pending = null;
            }

            if (portfolio.ActivePosition is not null)
            {
                var fill = fillResolver.Resolve(portfolio.ActivePosition, candle, request.ConflictPolicy);
                if (fill is not null)
                {
                    var price = costModel.ApplyExitSlippage(fill.Price, portfolio.ActivePosition.Side,  request.SlippageBasisPoints);
                    
                    portfolio.Close(Round(price, rules.TickSize), candle.CloseTimeUtc, fill.Reason);
                }
            }

            if (i >= strategy.WarmupBars)
            {
                var context = new BacktestStrategyContext
                {
                    CurrentCandle = candle,
                    PreviousCandle = i > 0 ? candles[i - 1] : null,
                    History = new ArraySegment<MarketCandle>(candles, 0, i + 1),
                    ActivePosition = portfolio.ActivePosition,
                    Balance = portfolio.Balance,
                    BarIndex = i
                };

                var decision = await strategy.DecideAsync(context, ct);
                if (decision.Type != DecisionType.None)
                {
                    if (decision.Type is DecisionType.Close or DecisionType.CloseAndReverse && portfolio.ActivePosition is not null)
                    {
                        var closePrice = costModel.ApplyExitSlippage(candle.Close, portfolio.ActivePosition.Side, request.SlippageBasisPoints);
                        
                        portfolio.Close(
                            Round(closePrice, rules.TickSize),
                            candle.CloseTimeUtc,
                            decision.Type == DecisionType.CloseAndReverse ? ExitReason.ReverseSignal : ExitReason.Strategy);
                    }

                    if (decision.Type is DecisionType.Open or DecisionType.CloseAndReverse
                        && decision.Side.HasValue
                        && decision.StopLoss.HasValue)
                    {
                        var marker = new SignalMarker(
                            candle.CloseTimeUtc,
                            decision.Side.Value,
                            candle.Close,
                            false,
                            decision.Reason);

                        if (request.EntryExecutionMode == EntryExecutionMode.SignalCandleClose)
                        {
                            var success = Open(
                                new PendingEntry(
                                    decision.Side.Value,
                                    decision.StopLoss.Value,
                                    decision.TakeProfit,
                                    -1,
                                    decision.Reason),
                                candle.Close,
                                candle.CloseTimeUtc);
                            signals.Add(marker with { Executed = success });
                        }
                        else if (i + 1 < candles.Length)
                        {
                            var markerIndex = signals.Count;
                            signals.Add(marker with { Reason = decision.Reason + " (pending next open)" });
                            pending = new PendingEntry(
                                decision.Side.Value,
                                decision.StopLoss.Value,
                                decision.TakeProfit,
                                markerIndex,
                                decision.Reason);
                        }
                        else
                        {
                            signals.Add(marker with { Reason = decision.Reason + " (no next candle)" });
                        }
                    }
                }
            }

            portfolio.MarkToMarket(candle.Close, candle.CloseTimeUtc);
        }

        if (portfolio.ActivePosition is not null)
        {
            var last = candles[^1];
            var price = costModel.ApplyExitSlippage(
                last.Close,
                portfolio.ActivePosition.Side,
                request.SlippageBasisPoints);
            portfolio.Close(Round(price, rules.TickSize), last.CloseTimeUtc, ExitReason.BacktestEnd);
        }

        var metrics = metricsCalculator.Calculate(
            portfolio.InitialBalance,
            portfolio.Balance,
            portfolio.Trades,
            portfolio.EquityCurve);

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
            if (portfolio.ActivePosition is not null)
                return false;

            var price = Round(
                costModel.ApplyEntrySlippage(rawPrice, entry.Side, request.SlippageBasisPoints),
                rules.TickSize);
            var stopLoss = Round(entry.StopLoss, rules.TickSize);
            var takeProfit = entry.TakeProfit.HasValue
                ? Round(entry.TakeProfit.Value, rules.TickSize)
                : (decimal?)null;
            var size = sizeCalculator.Calculate(
                portfolio.Balance,
                request.RiskPerTradePercent,
                price,
                stopLoss,
                rules);

            if (!size.Succeeded)
                return false;

            var risk = Math.Abs(price - stopLoss) * size.Quantity * rules.ContractMultiplier;
            try
            {
                portfolio.Open(
                    request.Symbol,
                    entry.Side,
                    price,
                    size.Quantity,
                    stopLoss,
                    takeProfit,
                    timeUtc,
                    risk);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    private static decimal Round(decimal value, decimal step)
        => step <= 0m
            ? value
            : Math.Round(value / step, MidpointRounding.AwayFromZero) * step;

    private static void Validate(IReadOnlyList<MarketCandle> candles)
    {
        if (candles.Count < 2)
            throw new InvalidOperationException("At least two candles are required.");

        for (var i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            if (!candle.IsClosed)
                throw new InvalidOperationException($"Backtesting requires closed candles. Candle = {candle.OpenTimeUtc:O}.");
            if (candle.CloseTimeUtc < candle.OpenTimeUtc)
                throw new InvalidOperationException($"Candle close time precedes open time. Candle = {candle.OpenTimeUtc:O}.");
            if (candle.Volume < 0
                || candle.Low > candle.High
                || candle.Open < candle.Low
                || candle.Open > candle.High
                || candle.Close < candle.Low
                || candle.Close > candle.High)
                throw new InvalidOperationException($"Invalid OHLCV candle at {candle.OpenTimeUtc:O}.");
            if (i > 0 && candle.OpenTimeUtc <= candles[i - 1].OpenTimeUtc)
                throw new InvalidOperationException("Candles must have unique ascending timestamps.");
        }
    }

    private sealed record PendingEntry(
        TradeSide Side,
        decimal StopLoss,
        decimal? TakeProfit,
        int MarkerIndex,
        string Reason);
}
