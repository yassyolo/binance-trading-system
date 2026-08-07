using Microsoft.Extensions.Options;

namespace TradingSystem.PortfolioManagement.Configuration;

public sealed class PortfolioOptionsValidator : IValidateOptions<PortfolioOptions>
{
    public ValidateOptionsResult Validate(string? name, PortfolioOptions options)
    {
        var errors = new List<string>();

        if (options.InitialEquity <= 0)
            errors.Add("InitialEquity must be greater than zero.");

        if (options.SnapshotCacheMilliseconds < 0)
            errors.Add("SnapshotCacheMilliseconds cannot be negative.");

        if (options.DefaultLeverage is < 1 or > 125)
            errors.Add("DefaultLeverage must be between 1 and 125.");

        if (options.LoadRetryCount is < 0 or > 10)
            errors.Add("LoadRetryCount must be between 0 and 10.");

        if (options.LoadRetryDelayMilliseconds is < 0 or > 30_000)
            errors.Add("LoadRetryDelayMilliseconds must be between 0 and 30000.");

        if (options.Bots.Any(string.IsNullOrWhiteSpace))
            errors.Add("Portfolio bot names cannot be empty.");

        if (options.Bots.Count != options.Bots.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            errors.Add("Portfolio bot names must be unique.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
