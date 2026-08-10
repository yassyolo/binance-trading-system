namespace TradingSystem.Application.Risk.Models;

public sealed record RiskStateSnapshot(
    decimal DailyRealizedPnl,
    decimal DailyPeakEquity,
    decimal CurrentEquity,
    int ConsecutiveLosses,
    bool HasCriticalReconciliationFindings);