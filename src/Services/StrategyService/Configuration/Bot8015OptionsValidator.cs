using Microsoft.Extensions.Options;

namespace StrategyService.Configuration;

public sealed class Bot8015OptionsValidator : IValidateOptions<Bot8015Options>
{
    public ValidateOptionsResult Validate(string? name, Bot8015Options options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BotName))
            errors.Add("BotName is required.");

        if (string.IsNullOrWhiteSpace(options.Symbol))
            errors.Add("Symbol is required.");

        if (options.Quantity <= 0)
            errors.Add("Quantity must be greater than zero.");

        if (options.Leverage <= 0)
            errors.Add("Leverage must be greater than zero.");

        if (options.InitialStopLoss <= 0)
            errors.Add("InitialStopLoss must be greater than zero.");

        if (options.TpPercent <= 0)
            errors.Add("TpPercent must be greater than zero.");

        if (options.OrderSideLimit <= 0)
            errors.Add("OrderSideLimit must be greater than zero.");

        if (options.CooldownSeconds < 0)
            errors.Add("CooldownSeconds cannot be negative.");

        if (options.Stop3TrailingStep <= 0)
            errors.Add("Stop3TrailingStep must be greater than zero.");

        if (options.Stop3TrailingBuffer <= 0)
            errors.Add("Stop3TrailingBuffer must be greater than zero.");

        if (options.HealingIntervalSeconds <= 0)
            errors.Add("HealingIntervalSeconds must be greater than zero.");

        if (options.TrailingCheckIntervalSeconds <= 0)
            errors.Add("TrailingCheckIntervalSeconds must be greater than zero.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
