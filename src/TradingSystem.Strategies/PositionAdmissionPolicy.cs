using TradingSystem.Domain.Enums;
namespace TradingSystem.Strategies.Positions;
public sealed record PositionAdmissionParameters(bool EnableLong, bool EnableShort, int SideLimit, bool CloseOppositeFirst);
public sealed record PositionAdmissionDecision(bool Allowed, IReadOnlyCollection<string> PositionsToClose, string Reason);
public sealed class PositionAdmissionPolicy
{
 public PositionAdmissionDecision Evaluate(PositionSide side, IReadOnlyCollection<(string Id, PositionSide Side)> active, PositionAdmissionParameters p)
 {
  if (side == PositionSide.Long && !p.EnableLong) return new(false, [], "LONG is disabled.");
  if (side == PositionSide.Short && !p.EnableShort) return new(false, [], "SHORT is disabled.");
  var opposite = active.Where(x => x.Side != side).Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  if (p.CloseOppositeFirst && opposite.Length > 0) return new(true, opposite, $"Close {opposite.Length} opposite position(s) first.");
  var same = active.Count(x => x.Side == side);
  return same >= p.SideLimit ? new(false, [], $"ORDER_SIDE_LIMIT reached ({same}/{p.SideLimit}).") : new(true, [], $"Admission valid. Active same side = {same}.");
 }
}