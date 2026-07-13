using Microsoft.Extensions.Options;

namespace StrategyService.Configuration;

public sealed class Bot8011OptionsValidator : IValidateOptions<Bot8011Options>
{
    public ValidateOptionsResult Validate(string? name, Bot8011Options options)
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
        if (options.InitialStopLossDistance <= 0)
            errors.Add("InitialStopLossDistance must be greater than zero.");
        if (options.TakeProfitPercent <= 0)
            errors.Add("TakeProfitPercent must be greater than zero.");
        if (options.CooldownSeconds < 0)
            errors.Add("CooldownSeconds cannot be negative.");
        if (options.OrderSideLimit <= 0)
            errors.Add("OrderSideLimit must be greater than zero.");
        if (options.Stop3TrailingStep <= 0)
            errors.Add("Stop3TrailingStep must be greater than zero.");
        if (options.Stop3TrailingBuffer <= 0)
            errors.Add("Stop3TrailingBuffer must be greater than zero.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
