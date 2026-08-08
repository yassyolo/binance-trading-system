namespace TradingSystem.Reconciliation.Configuration;

public sealed record ReconciliationOptions
{
    public const string SectionName = "Reconciliation";

    public bool Enabled { get; init; } = true;

    public int IntervalSeconds { get; init; } = 30;

    public bool AutoHealStaleLocalPositions { get; init; } = true;

    public bool AutoHealProtectiveOrders { get; init; }

    public decimal QuantityTolerance { get; init; } = 0.00000001m;

    public string[] Bots { get; init; } = [];

    public string[] Symbols { get; init; } = [];
}