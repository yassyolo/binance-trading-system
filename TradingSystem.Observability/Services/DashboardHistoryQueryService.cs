using TradingSystem.Observability.Abstractions;
using TradingSystem.Observability.Models;
namespace TradingSystem.Observability.Services;

public sealed record BotDashboardSummary(int Signals, int Opened, int Blocked, int TakeProfits, decimal RealizedPnl, DateTime? LastEventUtc);

public sealed class DashboardHistoryQueryService(ITradingHistoryStore store)
{
    public async Task<BotDashboardSummary> GetSummaryAsync(string botName, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var signals = 0; var opened = 0; var blocked = 0; var tps = 0; decimal pnl = 0; DateTime? last = null;
        await foreach (var e in store.QueryAsync(new TradingHistoryQuery { BotName = botName, FromUtc = fromUtc, ToUtc = toUtc }, ct))
        {
            if (e.Type == TradingHistoryEventType.SignalReceived) signals++;
            if (e.Type == TradingHistoryEventType.PositionOpened) opened++;
            if (e.Type == TradingHistoryEventType.StrategyDecision && e.Decision == "Block") blocked++;
            if (e.Type == TradingHistoryEventType.TakeProfitFilled) tps++;
            pnl += e.RealizedPnl ?? 0; if (last is null || e.OccurredAtUtc > last) last = e.OccurredAtUtc;
        }
        return new(signals, opened, blocked, tps, pnl, last);
    }
}
