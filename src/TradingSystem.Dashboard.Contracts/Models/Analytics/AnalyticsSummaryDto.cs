namespace TradingSystem.Dashboard.Contracts.Models.Analytics;

public sealed record AnalyticsSummaryDto(
    decimal TotalPnl,
    decimal DailyPnl, 
    decimal WeeklyPnl, 
    decimal MonthlyPnl, 
    decimal WinRate, 
    decimal AverageWin,
    decimal AverageLoss,
    decimal MaxDrawdownPercent, 
    decimal AverageHoldingMinutes, 
    int Signals,
    int OpenedSignals, 
    int BlockedSignals, 
    IReadOnlyDictionary<string, int> BlockReasons, 
    decimal LongPnl,
    decimal ShortPnl);