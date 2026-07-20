using TradingSystem.Domain.Enums;
namespace TradingSystem.Strategies.Alligator;
public sealed record AlligatorEntryInput(string Symbol, string Interval, bool IsClosed, decimal Open, decimal High, decimal Low, decimal Close, decimal Teeth, decimal Sma200);
public sealed record AlligatorEntryParameters(string Symbol, string Interval, bool EnableLong, bool EnableShort, bool UseMa200Filter, decimal MinimumCandleRange);
public sealed record AlligatorEntryDecision(PositionSide Side, string Reason);
public sealed class AlligatorEntryPolicy
{
 public AlligatorEntryDecision? Evaluate(AlligatorEntryInput x, AlligatorEntryParameters p)
 {
  if(!x.IsClosed || !x.Symbol.Equals(p.Symbol, StringComparison.OrdinalIgnoreCase) || !x.Interval.Equals(p.Interval, StringComparison.OrdinalIgnoreCase) || x.High-x.Low<p.MinimumCandleRange)return null;
  var bullish = x.Close>x.Open && x.Open<x.Teeth && x.Close>x.Teeth;
  if(bullish && p.EnableLong && (!p.UseMa200Filter || x.Close>=x.Sma200))return new(PositionSide.Long, $"Bullish candle crossed Teeth. Close = {x.Close},  Teeth = {x.Teeth},  SMA200 = {x.Sma200}.");
  var bearish = x.Close<x.Open && x.Open>x.Teeth && x.Close<x.Teeth;
  if(bearish && p.EnableShort && (!p.UseMa200Filter || x.Close<=x.Sma200))return new(PositionSide.Short, $"Bearish candle crossed Teeth. Close = {x.Close},  Teeth = {x.Teeth},  SMA200 = {x.Sma200}.");
  return null;
 }
}