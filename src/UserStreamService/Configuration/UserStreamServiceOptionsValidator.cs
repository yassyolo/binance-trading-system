using Microsoft.Extensions.Options;

namespace UserStreamService.Configuration;

public sealed class UserStreamServiceOptionsValidator
    : IValidateOptions<UserStreamServiceOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        UserStreamServiceOptions options)
    {
        var errors = new List<string>();

        if (options.ReconnectDelaySeconds <= 0)
        {
            errors.Add(
                "ReconnectDelaySeconds must be positive.");
        }

        if (options.MinDowntimeForHealingSeconds < 0)
        {
            errors.Add(
                "MinDowntimeForHealingSeconds cannot be negative.");
        }

        if (options.HealingCooldownSeconds < 0)
        {
            errors.Add(
                "HealingCooldownSeconds cannot be negative.");
        }

        if (options.HealingSymbols is null ||
            !options.HealingSymbols.Any(x =>
                !string.IsNullOrWhiteSpace(x)))
        {
            errors.Add(
                "At least one healing symbol is required.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}