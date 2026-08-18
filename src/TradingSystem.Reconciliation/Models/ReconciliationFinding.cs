using TradingSystem.Domain.Positions;
using TradingSystem.Reconciliation.Models.Enums;

namespace TradingSystem.Reconciliation.Models;

public sealed record ReconciliationFinding(
    Guid Id,
    DateTime DetectedAtUtc,
    string BotName, string Symbol,
    string? ShortId,
    ReconciliationFindingType Type,
    ReconciliationSeverity Severity,
    string Details, HealingActionType SuggestedAction, bool AutoHealAllowed)
{
    public static ReconciliationFinding New(
        BotPosition position,
        ReconciliationFindingType type,
        ReconciliationSeverity severity,
        string details,
        HealingActionType action,
        bool autoHealAllowed)
        => new(
            Guid.NewGuid(),
            DateTime.UtcNow,
            position.BotName,
            position.Symbol,
            position.ShortId,
            type,
            severity,
            details,
            action,
            autoHealAllowed);
}