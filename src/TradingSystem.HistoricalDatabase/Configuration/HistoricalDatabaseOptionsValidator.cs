using Microsoft.Extensions.Options;

namespace TradingSystem.HistoricalDatabase.Configuration;

public sealed class HistoricalDatabaseOptionsValidator : IValidateOptions<HistoricalDatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, HistoricalDatabaseOptions options)
    {
        var e = new List<string>();
        
        if (string.IsNullOrWhiteSpace(options.ConnectionStringName))
            e.Add("ConnectionStringName is required.");
       
        if (options.CommandTimeoutSeconds <= 0)
            e.Add("CommandTimeoutSeconds must be positive.");
        
        if (options.RetentionDays < 30)
            e.Add("RetentionDays must be at least 30.");
        
        if (options.CleanupIntervalHours <= 0)
            e.Add("CleanupIntervalHours must be positive.");
       
        if (options.PortfolioSnapshotIntervalSeconds < 5)
            e.Add("PortfolioSnapshotIntervalSeconds must be at least 5.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
