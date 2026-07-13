using Microsoft.Extensions.Options;

namespace StrategyService.Configuration;

public sealed class Bot8014OptionsValidator : IValidateOptions<Bot8014Options>
{
    public ValidateOptionsResult Validate(
        string? name,
        Bot8014Options options)
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

        if (options.PriceDistance <= 0)
            errors.Add("PriceDistance must be greater than zero.");

        if (options.ProfitDistance <= 0)
            errors.Add("ProfitDistance must be greater than zero.");

        if (options.OrderSideLimit <= 0)
            errors.Add("OrderSideLimit must be greater than zero.");

        if (options.CooldownSeconds < 0)
            errors.Add("CooldownSeconds cannot be negative.");

        if (options.HealingIntervalSeconds <= 0)
            errors.Add("HealingIntervalSeconds must be greater than zero.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
