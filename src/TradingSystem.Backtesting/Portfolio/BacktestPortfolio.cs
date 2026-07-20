using TradingSystem.Backtesting.Costs;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Portfolio;

public sealed class BacktestPortfolio(decimal initialBalance,  SymbolTradingRules rules,  ITradingCostModel costModel)
{
    private readonly List<BacktestTrade> _trades  =  [];
    private readonly List<EquityPoint> _equity  =  [];
    private long _nextId  =  1;
    private decimal _peakBalance  =  initialBalance;

    public decimal InitialBalance {  get;  }  =  initialBalance;
    public decimal Balance {  get;  private set; }  =  initialBalance;
    public BacktestPosition? ActivePosition {  get;  private set; }
    public IReadOnlyList<BacktestTrade> Trades  =>  _trades;
    public IReadOnlyList<EquityPoint> EquityCurve  =>  _equity;

    public BacktestPosition Open(string symbol,  TradeSide side,  decimal entryPrice,  decimal quantity,  decimal stopLoss,  decimal? takeProfit,  DateTime timeUtc,  decimal initialRisk)
    {
        if (ActivePosition is not null) throw new InvalidOperationException("Only one active position is supported in v1.");
        var fee  =  costModel.CalculateEntryFee(entryPrice,  quantity);
        Balance -= fee;
        ActivePosition  =  new BacktestPosition
        {
            Id  =  _nextId++,  Symbol  =  symbol,  Side  =  side,  EntryPrice  =  entryPrice,  Quantity  =  quantity, 
            EntryTimeUtc  =  timeUtc,  EntryFee  =  fee,  StopLoss  =  stopLoss,  TakeProfit  =  takeProfit,  InitialRiskAmount  =  initialRisk
        };
        RecordEquity(timeUtc);
        return ActivePosition;
    }

    public BacktestTrade Close(decimal exitPrice,  DateTime timeUtc,  ExitReason reason)
    {
        var position  =  ActivePosition ?? throw new InvalidOperationException("No active position.");
        var direction  =  position.Side == TradeSide.Long ? 1m : -1m;
        var gross  =  (exitPrice - position.EntryPrice) * position.Quantity * rules.ContractMultiplier * direction;
        var exitFee  =  costModel.CalculateExitFee(exitPrice,  position.Quantity);
        var funding  =  costModel.CalculateFunding(position,  timeUtc);
        var net  =  gross - exitFee - funding;
        Balance += net;
        var trade  =  new BacktestTrade
        {
            Id  =  position.Id,  Symbol  =  position.Symbol,  Side  =  position.Side, 
            EntryTimeUtc  =  position.EntryTimeUtc,  ExitTimeUtc  =  timeUtc, 
            EntryPrice  =  position.EntryPrice,  ExitPrice  =  exitPrice,  Quantity  =  position.Quantity, 
            ExitReason  =  reason,  GrossPnl  =  gross,  EntryFee  =  position.EntryFee, 
            ExitFee  =  exitFee,  FundingCost  =  funding,  NetPnl  =  gross - position.EntryFee - exitFee - funding, 
            RMultiple  =  position.InitialRiskAmount > 0m ? (gross - position.EntryFee - exitFee - funding) / position.InitialRiskAmount : 0m
        };
        _trades.Add(trade);
        ActivePosition  =  null;
        RecordEquity(timeUtc);
        return trade;
    }

    public void RecordEquity(DateTime timeUtc)
    {
        _peakBalance  =  Math.Max(_peakBalance,  Balance);
        var drawdown  =  _peakBalance - Balance;
        var pct  =  _peakBalance > 0m ? drawdown / _peakBalance * 100m : 0m;
        _equity.Add(new EquityPoint(timeUtc,  Balance,  drawdown,  pct));
    }
}
