namespace TradingSystem.Operations.Configuration;

public sealed class ServiceHeartbeatOptions
{
    public const string SectionName = "ServiceHeartbeat";

    public bool Enabled { get; set; } = true;

    public string ServiceName { get; set; } = "UnknownService";

    public string Environment { get; set; } = "Demo";

    public int IntervalSeconds { get; set; } = 10;

    public int StaleAfterSeconds { get; set; } = 30;
}
