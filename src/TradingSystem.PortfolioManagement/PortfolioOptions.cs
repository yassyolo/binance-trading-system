using Microsoft.Extensions.Options;

namespace TradingSystem.PortfolioManagement;

public sealed class PortfolioOptions
{
    public const string SectionName = "Portfolio";

    public bool Enabled { get; set; } = true;
    public decimal InitialEquity { get; set; } = 10_000m;
    public int SnapshotCacheMilliseconds { get; set; } = 500;
    public int DefaultLeverage { get; set; } = 1;
    public List<string> Bots { get; set; } = [];
}

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

        if (options.Bots.Any(string.IsNullOrWhiteSpace))
            errors.Add("Portfolio bot names cannot be empty.");

        if (options.Bots.Count != options.Bots.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            errors.Add("Portfolio bot names must be unique.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
