using Microsoft.Extensions.Options;

namespace TradingSystem.PortfolioManagement.Configuration;

public sealed class PortfolioOptionsValidator : IValidateOptions<PortfolioOptions>
{
    public ValidateOptionsResult Validate(string? name, PortfolioOptions options)
    {
        var e = new List<string>();

        if (options.InitialEquity <= 0)
            e.Add("InitialEquity must be greater than zero.");

        if (options.SnapshotCacheMilliseconds < 0)
            e.Add("SnapshotCacheMilliseconds cannot be negative.");

        if (options.DefaultLeverage is < 1 or > 125)
            e.Add("DefaultLeverage must be between 1 and 125.");

        if (options.LoadRetryCount is < 0 or > 10)
            e.Add("LoadRetryCount must be between 0 and 10.");

        if (options.LoadRetryDelayMilliseconds is < 0 or > 30_000)
            e.Add("LoadRetryDelayMilliseconds must be between 0 and 30000.");

        if (options.Bots.Any(string.IsNullOrWhiteSpace))
            e.Add("Portfolio bot names cannot be empty.");

        if (options.Bots.Count != options.Bots.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            e.Add("Portfolio bot names must be unique.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}
