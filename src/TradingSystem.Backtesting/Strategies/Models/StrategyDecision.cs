using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Strategies.Models;

public sealed record StrategyDecision
{
    public DecisionType Type { get; init; }
    public TradeSide? Side { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public string Reason { get; init; }  =  string.Empty;

    public static StrategyDecision None(string reason  =  "")  =>  new() { Type  =  DecisionType.None,  Reason  =  reason };
    public static StrategyDecision Open(TradeSide side,  decimal stopLoss,  decimal? takeProfit,  string reason)
         =>  new() { Type  =  DecisionType.Open,  Side  =  side,  StopLoss  =  stopLoss,  TakeProfit  =  takeProfit,  Reason  =  reason };
    public static StrategyDecision Close(string reason)
         =>  new() { Type  =  DecisionType.Close,  Reason  =  reason };
    public static StrategyDecision Reverse(TradeSide side,  decimal stopLoss,  decimal? takeProfit,  string reason)
         =>  new() { Type  =  DecisionType.CloseAndReverse,  Side  =  side,  StopLoss  =  stopLoss,  TakeProfit  =  takeProfit,  Reason  =  reason };
}
