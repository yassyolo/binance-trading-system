using TradingSystem.Domain.Enums;

namespace TradingSystem.Strategies.Alligator.Models;

public sealed record AlligatorEntryDecision(PositionSide Side, string Reason);

