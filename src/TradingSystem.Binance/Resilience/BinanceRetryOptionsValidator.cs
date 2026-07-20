using Microsoft.Extensions.Options;

namespace TradingSystem.Binance.Resilience;

public sealed class BinanceRetryOptionsValidator : IValidateOptions<BinanceRetryOptions>
{
    public ValidateOptionsResult Validate(string? name, BinanceRetryOptions options)
    {
        var errors = new List<string>();

        if (options.MaximumAttempts is < 1 or > 10)
            errors.Add("BinanceRetry:MaximumAttempts must be between 1 and 10.");

        if (options.InitialDelayMilliseconds is < 0 or > 60_000)
            errors.Add("BinanceRetry:InitialDelayMilliseconds must be between 0 and 60000.");

        if (options.MaximumDelayMilliseconds is < 1 or > 120_000)
            errors.Add("BinanceRetry:MaximumDelayMilliseconds must be between 1 and 120000.");

        if (options.MaximumDelayMilliseconds < options.InitialDelayMilliseconds)
            errors.Add("BinanceRetry:MaximumDelayMilliseconds must be greater than or equal to InitialDelayMilliseconds.");

        if (options.BackoffMultiplier is < 1 or > 10)
            errors.Add("BinanceRetry:BackoffMultiplier must be between 1 and 10.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
