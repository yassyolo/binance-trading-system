using Microsoft.Extensions.Options;

namespace TradingSystem.HistoricalDatabase;

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

public sealed class HistoricalDatabaseOptionsValidator : IValidateOptions<HistoricalDatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, HistoricalDatabaseOptions options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
            errors.Add("ConnectionStringName is required.");
        if (options.CommandTimeoutSeconds <= 0)
            errors.Add("CommandTimeoutSeconds must be positive.");
        if (options.RetentionDays < 30)
            errors.Add("RetentionDays must be at least 30.");
        if (options.CleanupIntervalHours <= 0)
            errors.Add("CleanupIntervalHours must be positive.");
        if (options.PortfolioSnapshotIntervalSeconds < 5)
            errors.Add("PortfolioSnapshotIntervalSeconds must be at least 5.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}