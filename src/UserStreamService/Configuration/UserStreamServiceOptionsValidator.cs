using Microsoft.Extensions.Options;

namespace UserStreamService.Configuration;

public sealed class UserStreamServiceOptionsValidator : IValidateOptions<UserStreamServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, UserStreamServiceOptions options)
    {
        var e = new List<string>();

        if (options.ReconnectDelaySeconds <= 0)
            e.Add("ReconnectDelaySeconds must be positive.");

        if (options.MinDowntimeForHealingSeconds < 0)
            e.Add("MinDowntimeForHealingSeconds cannot be negative.");

        if (options.HealingCooldownSeconds < 0)
            e.Add("HealingCooldownSeconds cannot be negative.");

        if (options.HealingSymbols is null || !options.HealingSymbols.Any(x => !string.IsNullOrWhiteSpace(x)))
            e.Add("At least one healing symbol is required.");

        return e.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(e);
    }
}