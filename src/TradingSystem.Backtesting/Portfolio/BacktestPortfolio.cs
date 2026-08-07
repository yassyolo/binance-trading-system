using TradingSystem.Backtesting.Costs.Contracts;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Portfolio;

public sealed class BacktestPortfolio(
    decimal initialBalance,
    SymbolTradingRules rules,
    ITradingCostModel costModel)
{
    private readonly List<BacktestTrade> _trades = [];
    private readonly List<EquityPoint> _equity = [];
    private long _nextId = 1;
    private decimal _peakEquity = initialBalance;

    public decimal InitialBalance { get; } = initialBalance;
    public decimal Balance { get; private set; } = initialBalance;
    public BacktestPosition? ActivePosition { get; private set; }
    public IReadOnlyList<BacktestTrade> Trades => _trades;
    public IReadOnlyList<EquityPoint> EquityCurve => _equity;

    public BacktestPosition Open(
        string symbol,
        TradeSide side,
        decimal entryPrice,
        decimal quantity,
        decimal stopLoss,
        decimal? takeProfit,
        DateTime timeUtc,
        decimal initialRisk)
    {
        if (ActivePosition is not null)
            throw new InvalidOperationException("Only one active position is supported in v1.");

        var requiredMargin = entryPrice * quantity * rules.ContractMultiplier / rules.Leverage;
        var entryFee = costModel.CalculateEntryFee(entryPrice, quantity);

        if (requiredMargin + entryFee > Balance)
            throw new InvalidOperationException("Available balance is insufficient for margin and entry fee.");

        Balance -= entryFee;
        ActivePosition = new BacktestPosition
        {
            Id = _nextId++,
            Symbol = symbol,
            Side = side,
            EntryPrice = entryPrice,
            Quantity = quantity,
            EntryTimeUtc = timeUtc,
            EntryFee = entryFee,
            StopLoss = stopLoss,
            TakeProfit = takeProfit,
            InitialRiskAmount = initialRisk
        };

        MarkToMarket(entryPrice, timeUtc);
        return ActivePosition;
    }

    public BacktestTrade Close(decimal exitPrice, DateTime timeUtc, ExitReason reason)
    {
        var position = ActivePosition ?? throw new InvalidOperationException("No active position.");
        var direction = position.Side == TradeSide.Long ? 1m : -1m;
        var gross = (exitPrice - position.EntryPrice) * position.Quantity * rules.ContractMultiplier * direction;
        var exitFee = costModel.CalculateExitFee(exitPrice, position.Quantity);
        var funding = costModel.CalculateFunding(position, timeUtc);
        var netAfterExit = gross - exitFee - funding;

        Balance += netAfterExit;

        var trade = new BacktestTrade
        {
            Id = position.Id,
            Symbol = position.Symbol,
            Side = position.Side,
            EntryTimeUtc = position.EntryTimeUtc,
            ExitTimeUtc = timeUtc,
            EntryPrice = position.EntryPrice,
            ExitPrice = exitPrice,
            Quantity = position.Quantity,
            ExitReason = reason,
            GrossPnl = gross,
            EntryFee = position.EntryFee,
            ExitFee = exitFee,
            FundingCost = funding,
            NetPnl = gross - position.EntryFee - exitFee - funding,
            RMultiple = position.InitialRiskAmount > 0m
                ? (gross - position.EntryFee - exitFee - funding) / position.InitialRiskAmount
                : 0m
        };

        _trades.Add(trade);
        ActivePosition = null;
        MarkToMarket(exitPrice, timeUtc);
        return trade;
    }

    public void MarkToMarket(decimal marketPrice, DateTime timeUtc)
    {
        var unrealized = ActivePosition is null
            ? 0m
            : UnrealizedPnl(ActivePosition, marketPrice);

        var equity = Balance + unrealized;
        _peakEquity = Math.Max(_peakEquity, equity);
        var drawdown = _peakEquity - equity;
        var drawdownPercent = _peakEquity > 0m
            ? drawdown / _peakEquity * 100m
            : 0m;

        if (_equity.Count > 0 && _equity[^1].TimeUtc == timeUtc)
            _equity[^1] = new(timeUtc, Balance, equity, _peakEquity, drawdown, drawdownPercent);
        else
            _equity.Add(new(timeUtc, Balance, equity, _peakEquity, drawdown, drawdownPercent));
    }

    private decimal UnrealizedPnl(BacktestPosition position, decimal currentPrice)
    {
        var direction = position.Side == TradeSide.Long ? 1m : -1m;
        return (currentPrice - position.EntryPrice)
               * position.Quantity
               * rules.ContractMultiplier
               * direction;
    }
}
