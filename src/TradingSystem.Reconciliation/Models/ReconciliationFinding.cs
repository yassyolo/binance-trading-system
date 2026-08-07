using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingSystem.Reconciliation.Models.Enums;

namespace TradingSystem.Reconciliation.Models;

public sealed record ReconciliationFinding(
    Guid Id,
    DateTime DetectedAtUtc,
    string BotName, string Symbol,
    string? ShortId,
    ReconciliationFindingType Type,
    ReconciliationSeverity Severity,
    string Details, HealingActionType SuggestedAction, bool AutoHealAllowed);