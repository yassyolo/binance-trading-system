using TradingSystem.Application.Positions.Models;
using TradingSystem.Domain.Signals;

namespace TradingSystem.Application.Risk.Models;

public sealed record RiskStateSnapshot(
    decimal DailyRealizedPnl,
    decimal DailyPeakEquity,
    decimal CurrentEquity,
    int ConsecutiveLosses,
    bool HasCriticalReconciliationFindings);