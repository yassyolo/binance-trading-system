using Microsoft.Extensions.Options;

namespace TradingSystem.HistoricalDatabase.Configuration;

public sealed class HistoricalDatabaseOptions
{
    public const string SectionName = "HistoricalDatabase";

    public bool Enabled { get; set; } = true;
    public string ConnectionStringName { get; set; } = "TradingDatabase";
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int RetentionDays { get; set; } = 365;
    public int CleanupIntervalHours { get; set; } = 24;
    public int PortfolioSnapshotIntervalSeconds { get; set; } = 30;
    public bool StoreRawPayloads { get; set; } = true;
}