using TradingSystem.Dashboard.Contracts.Models.Bots;

namespace TradingSystem.Dashboard.Contracts.Models.Health;

public sealed record LiveOverviewDto(
    DateTime GeneratedAtUtc, 
    IReadOnlyCollection<BotOverviewDto> Bots,
    IReadOnlyCollection<ComponentHealthDto> Components,
    decimal RealizedPnlToday,
    decimal UnrealizedPnl, 
    int OpenPositions,
    int CriticalAlerts);
