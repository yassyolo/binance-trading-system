using Microsoft.Extensions.Options;

namespace TradingSystem.PortfolioManagement.Configuration;

public sealed class PortfolioOptions
{
    public const string SectionName = "Portfolio";

    public bool Enabled { get; set; } = true;
    public decimal InitialEquity { get; set; } = 10_000m;
    public int SnapshotCacheMilliseconds { get; set; } = 500;
    public int DefaultLeverage { get; set; } = 1;
    public int LoadRetryCount { get; set; } = 2;
    public int LoadRetryDelayMilliseconds { get; set; } = 250;
    public List<string> Bots { get; set; } = [];
}
