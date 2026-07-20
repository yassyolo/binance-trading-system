using TradingSystem.Domain.Enums;
namespace TradingSystem.Strategies.Grid;
public sealed record GridPositionReference(PositionSide Side,  decimal TakeProfitPrice,  DateTime OpenedAtUtc);
public sealed record GridSpacingParameters(decimal PriceDistance,  decimal ProfitDistance,  int SideLimit);
public sealed record PolicyDecision(bool Allowed,  string Reason)
{ public static PolicyDecision Allow(string reason) => new(true, reason); public static PolicyDecision Block(string reason) => new(false, reason); }
public sealed class GridSpacingPolicy
{
 public PolicyDecision Evaluate(PositionSide side,  decimal markPrice,  IReadOnlyCollection<GridPositionReference> positions,  GridSpacingParameters parameters)
 {
  var same = positions.Where(x => x.Side==side).OrderByDescending(x => x.OpenedAtUtc).ToArray();
  if(same.Length>=parameters.SideLimit)return PolicyDecision.Block($"ORDER_SIDE_LIMIT reached ({same.Length}/{parameters.SideLimit}).");
  if(same.Length==0)return PolicyDecision.Allow("No active TP positions for this side.");
  var newestTp = Round(same[0].TakeProfitPrice); var mark = Round(markPrice); var profit = Round(parameters.ProfitDistance); var gap = Round(parameters.PriceDistance);
  if(side==PositionSide.Long){var maximum = newestTp-profit-gap;return mark<=maximum?PolicyDecision.Allow($"LONG spacing valid: mark = {mark},  requiredMaximum = {maximum}."):PolicyDecision.Block($"GAP fail LONG: mark = {mark},  requiredMaximum = {maximum}.");}
  var minimum = newestTp+profit+gap;return mark>=minimum?PolicyDecision.Allow($"SHORT spacing valid: mark = {mark},  requiredMinimum = {minimum}."):PolicyDecision.Block($"GAP fail SHORT: mark = {mark},  requiredMinimum = {minimum}.");
 }
 private static decimal Round(decimal value) => Math.Round(value, 0, MidpointRounding.ToEven);
}